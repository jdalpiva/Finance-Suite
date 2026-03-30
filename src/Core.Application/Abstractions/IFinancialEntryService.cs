using SMEFinanceSuite.Core.Application.FinancialEntries;

namespace SMEFinanceSuite.Core.Application.Abstractions;

public interface IFinancialEntryService
{
    Task<Guid> RegisterAsync(CreateFinancialEntryCommand command, CancellationToken cancellationToken = default);

    Task UpdateAsync(UpdateFinancialEntryCommand command, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialEntryListItemDto>> ListAsync(
        FinancialEntriesFilter? filter = null,
        CancellationToken cancellationToken = default);
}
