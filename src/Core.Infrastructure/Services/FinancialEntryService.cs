using Microsoft.EntityFrameworkCore;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.FinancialEntries;
using SMEFinanceSuite.Core.Domain.Entities;
using SMEFinanceSuite.Core.Infrastructure.Persistence;

namespace SMEFinanceSuite.Core.Infrastructure.Services;

public sealed class FinancialEntryService(IDbContextFactory<AppDbContext> dbContextFactory) : IFinancialEntryService
{
    public async Task<Guid> RegisterAsync(CreateFinancialEntryCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (command.CustomerId.HasValue)
        {
            bool customerExists = await dbContext.Customers
                .AsNoTracking()
                .AnyAsync(customer => customer.Id == command.CustomerId.Value, cancellationToken);

            if (!customerExists)
            {
                throw new InvalidOperationException("Cliente informado não foi encontrado.");
            }
        }

        if (command.ProductServiceId.HasValue)
        {
            bool productServiceExists = await dbContext.ProductsServices
                .AsNoTracking()
                .AnyAsync(productService => productService.Id == command.ProductServiceId.Value, cancellationToken);

            if (!productServiceExists)
            {
                throw new InvalidOperationException("Produto/serviço informado não foi encontrado.");
            }
        }

        FinancialEntry entry = new(
            description: command.Description,
            amount: command.Amount,
            occurredOn: command.OccurredOn,
            entryType: command.EntryType,
            customerId: command.CustomerId,
            productServiceId: command.ProductServiceId,
            notes: command.Notes);

        dbContext.FinancialEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        return entry.Id;
    }

    public async Task UpdateAsync(UpdateFinancialEntryCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        FinancialEntry? entry = await dbContext.FinancialEntries
            .FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);

        if (entry is null)
        {
            throw new InvalidOperationException("Lançamento informado não foi encontrado.");
        }

        entry.Update(
            description: command.Description,
            amount: command.Amount,
            occurredOn: command.OccurredOn,
            entryType: command.EntryType,
            notes: command.Notes);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        FinancialEntry? entry = await dbContext.FinancialEntries
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (entry is null)
        {
            throw new InvalidOperationException("Lançamento informado não foi encontrado.");
        }

        dbContext.FinancialEntries.Remove(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialEntryListItemDto>> ListAsync(
        FinancialEntriesFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        filter ??= new FinancialEntriesFilter();

        if (filter.From is not null && filter.To is not null && filter.From > filter.To)
        {
            throw new ArgumentException("A data inicial não pode ser maior que a data final.");
        }

        int maxResults = Math.Clamp(filter.MaxResults, 1, 500);

        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<FinancialEntry> query = dbContext.FinancialEntries.AsNoTracking();

        if (filter.From is not null)
        {
            query = query.Where(entry => entry.OccurredOn >= filter.From.Value);
        }

        if (filter.To is not null)
        {
            query = query.Where(entry => entry.OccurredOn <= filter.To.Value);
        }

        if (filter.EntryType is not null)
        {
            query = query.Where(entry => entry.EntryType == filter.EntryType.Value);
        }

        List<FinancialEntryListItemDto> entries = await query
            .OrderByDescending(entry => entry.OccurredOn)
            .ThenByDescending(entry => entry.CreatedAtUtc)
            .Take(maxResults)
            .Select(entry => new FinancialEntryListItemDto(
                Id: entry.Id,
                Description: entry.Description,
                Amount: entry.Amount,
                OccurredOn: entry.OccurredOn,
                EntryType: entry.EntryType,
                CustomerId: entry.CustomerId,
                ProductServiceId: entry.ProductServiceId,
                Notes: entry.Notes))
            .ToListAsync(cancellationToken);

        return entries;
    }
}
