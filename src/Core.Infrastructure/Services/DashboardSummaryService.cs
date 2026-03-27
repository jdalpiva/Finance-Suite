using Microsoft.EntityFrameworkCore;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.Dashboard;
using SMEFinanceSuite.Core.Domain.Enums;
using SMEFinanceSuite.Core.Infrastructure.Persistence;

namespace SMEFinanceSuite.Core.Infrastructure.Services;

public sealed class DashboardSummaryService(IDbContextFactory<AppDbContext> dbContextFactory) : IFinancialDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Core.Domain.Entities.FinancialEntry> query = dbContext.FinancialEntries.AsNoTracking();

        if (from is not null)
        {
            query = query.Where(entry => entry.OccurredOn >= from.Value);
        }

        if (to is not null)
        {
            query = query.Where(entry => entry.OccurredOn <= to.Value);
        }

        decimal totalRevenue = await query
            .Where(entry => entry.EntryType == EntryType.Revenue)
            .SumAsync(entry => (decimal?)entry.Amount, cancellationToken) ?? 0m;

        decimal totalExpense = await query
            .Where(entry => entry.EntryType == EntryType.Expense)
            .SumAsync(entry => (decimal?)entry.Amount, cancellationToken) ?? 0m;

        int customersCount = await dbContext.Customers.AsNoTracking().CountAsync(cancellationToken);
        int productsCount = await dbContext.ProductsServices.AsNoTracking().CountAsync(cancellationToken);

        return new DashboardSummaryDto(
            TotalRevenue: totalRevenue,
            TotalExpense: totalExpense,
            NetCashFlow: totalRevenue - totalExpense,
            CustomersCount: customersCount,
            ProductsCount: productsCount);
    }
}
