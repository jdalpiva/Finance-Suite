using Microsoft.EntityFrameworkCore;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.Reports;
using SMEFinanceSuite.Core.Domain.Enums;
using SMEFinanceSuite.Core.Infrastructure.Persistence;

namespace SMEFinanceSuite.Core.Infrastructure.Services;

public sealed class FinancialReportsService(IDbContextFactory<AppDbContext> dbContextFactory) : IFinancialReportsService
{
    public async Task<FinancialReportSummaryDto> GetSummaryAsync(
        FinancialReportFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        filter ??= new FinancialReportFilter();

        if (filter.From is not null && filter.To is not null && filter.From > filter.To)
        {
            throw new ArgumentException("A data inicial não pode ser maior que a data final.");
        }

        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Core.Domain.Entities.FinancialEntry> query = dbContext.FinancialEntries.AsNoTracking();

        if (filter.From is not null)
        {
            query = query.Where(entry => entry.OccurredOn >= filter.From.Value);
        }

        if (filter.To is not null)
        {
            query = query.Where(entry => entry.OccurredOn <= filter.To.Value);
        }

        decimal totalRevenue = await query
            .Where(entry => entry.EntryType == EntryType.Revenue)
            .SumAsync(entry => (decimal?)entry.Amount, cancellationToken) ?? 0m;

        decimal totalExpense = await query
            .Where(entry => entry.EntryType == EntryType.Expense)
            .SumAsync(entry => (decimal?)entry.Amount, cancellationToken) ?? 0m;

        IReadOnlyList<FinancialReportMonthlyBreakdownItemDto> monthlyBreakdown = (await query
            .Select(entry => new MonthlyProjection(entry.OccurredOn, entry.EntryType, entry.Amount))
            .ToListAsync(cancellationToken))
            .GroupBy(entry => new { entry.OccurredOn.Year, entry.OccurredOn.Month })
            .Select(group =>
            {
                decimal monthlyRevenue = group
                    .Where(entry => entry.EntryType == EntryType.Revenue)
                    .Sum(entry => entry.Amount);
                decimal monthlyExpense = group
                    .Where(entry => entry.EntryType == EntryType.Expense)
                    .Sum(entry => entry.Amount);

                return new FinancialReportMonthlyBreakdownItemDto(
                    Year: group.Key.Year,
                    Month: group.Key.Month,
                    TotalRevenue: monthlyRevenue,
                    TotalExpense: monthlyExpense,
                    NetBalance: monthlyRevenue - monthlyExpense);
            })
            .OrderBy(item => item.Year)
            .ThenBy(item => item.Month)
            .ToList();

        List<BreakdownRow> customerRows = (await query
            .GroupBy(entry => entry.CustomerId)
            .Select(group => new
            {
                ReferenceId = group.Key,
                TotalRevenue = group
                    .Where(entry => entry.EntryType == EntryType.Revenue)
                    .Sum(entry => (decimal?)entry.Amount) ?? 0m,
                TotalExpense = group
                    .Where(entry => entry.EntryType == EntryType.Expense)
                    .Sum(entry => (decimal?)entry.Amount) ?? 0m
            })
            .ToListAsync(cancellationToken))
            .Select(group => new BreakdownRow(group.ReferenceId, group.TotalRevenue, group.TotalExpense))
            .OrderByDescending(group => group.TotalRevenue - group.TotalExpense)
            .ThenByDescending(group => group.TotalRevenue)
            .ToList();

        List<Guid> customerIds = customerRows
            .Where(row => row.ReferenceId.HasValue)
            .Select(row => row.ReferenceId!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, string> customerNames = await dbContext.Customers
            .AsNoTracking()
            .Where(customer => customerIds.Contains(customer.Id))
            .ToDictionaryAsync(customer => customer.Id, customer => customer.Name, cancellationToken);

        List<BreakdownRow> productRows = (await query
            .GroupBy(entry => entry.ProductServiceId)
            .Select(group => new
            {
                ReferenceId = group.Key,
                TotalRevenue = group
                    .Where(entry => entry.EntryType == EntryType.Revenue)
                    .Sum(entry => (decimal?)entry.Amount) ?? 0m,
                TotalExpense = group
                    .Where(entry => entry.EntryType == EntryType.Expense)
                    .Sum(entry => (decimal?)entry.Amount) ?? 0m
            })
            .ToListAsync(cancellationToken))
            .Select(group => new BreakdownRow(group.ReferenceId, group.TotalRevenue, group.TotalExpense))
            .OrderByDescending(group => group.TotalRevenue - group.TotalExpense)
            .ThenByDescending(group => group.TotalRevenue)
            .ToList();

        List<Guid> productServiceIds = productRows
            .Where(row => row.ReferenceId.HasValue)
            .Select(row => row.ReferenceId!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, ProductSnapshot> productSnapshots = await dbContext.ProductsServices
            .AsNoTracking()
            .Where(item => productServiceIds.Contains(item.Id))
            .ToDictionaryAsync(
                item => item.Id,
                item => new ProductSnapshot(item.Name, item.Category),
                cancellationToken);

        IReadOnlyList<FinancialReportBreakdownItemDto> customerBreakdown = customerRows
            .Select(row => new FinancialReportBreakdownItemDto(
                ReferenceId: row.ReferenceId,
                Label: ResolveCustomerLabel(row.ReferenceId, customerNames),
                Category: null,
                TotalRevenue: row.TotalRevenue,
                TotalExpense: row.TotalExpense,
                NetBalance: row.TotalRevenue - row.TotalExpense))
            .ToList();

        IReadOnlyList<FinancialReportBreakdownItemDto> productBreakdown = productRows
            .Select(row =>
            {
                ProductSnapshot? snapshot = ResolveProductSnapshot(row.ReferenceId, productSnapshots);

                return new FinancialReportBreakdownItemDto(
                    ReferenceId: row.ReferenceId,
                    Label: ResolveProductLabel(row.ReferenceId, snapshot),
                    Category: snapshot?.Category,
                    TotalRevenue: row.TotalRevenue,
                    TotalExpense: row.TotalExpense,
                    NetBalance: row.TotalRevenue - row.TotalExpense);
            })
            .ToList();

        return new FinancialReportSummaryDto(
            From: filter.From,
            To: filter.To,
            TotalRevenue: totalRevenue,
            TotalExpense: totalExpense,
            NetBalance: totalRevenue - totalExpense,
            BreakdownByMonth: monthlyBreakdown,
            BreakdownByCustomer: customerBreakdown,
            BreakdownByProductService: productBreakdown);
    }

    private static string ResolveCustomerLabel(Guid? customerId, IReadOnlyDictionary<Guid, string> customerNames)
    {
        if (customerId is null)
        {
            return "Sem cliente";
        }

        return customerNames.TryGetValue(customerId.Value, out string? name)
            ? name
            : "Cliente removido";
    }

    private static ProductSnapshot? ResolveProductSnapshot(Guid? productServiceId, IReadOnlyDictionary<Guid, ProductSnapshot> productSnapshots)
    {
        if (productServiceId is null)
        {
            return null;
        }

        return productSnapshots.TryGetValue(productServiceId.Value, out ProductSnapshot? snapshot)
            ? snapshot
            : null;
    }

    private static string ResolveProductLabel(Guid? productServiceId, ProductSnapshot? snapshot)
    {
        if (productServiceId is null)
        {
            return "Sem produto/serviço";
        }

        return snapshot?.Name ?? "Item removido";
    }

    private sealed record BreakdownRow(Guid? ReferenceId, decimal TotalRevenue, decimal TotalExpense);

    private sealed record MonthlyProjection(DateOnly OccurredOn, EntryType EntryType, decimal Amount);

    private sealed record ProductSnapshot(string Name, string Category);
}
