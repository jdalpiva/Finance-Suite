using SMEFinanceSuite.Core.Domain.Enums;

namespace SMEFinanceSuite.Core.Application.FinancialEntries;

public sealed record FinancialEntryListItemDto(
    Guid Id,
    string Description,
    decimal Amount,
    DateOnly OccurredOn,
    EntryType EntryType,
    Guid? CustomerId,
    Guid? ProductServiceId,
    string? Notes);
