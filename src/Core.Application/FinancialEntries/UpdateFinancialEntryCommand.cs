using SMEFinanceSuite.Core.Domain.Enums;

namespace SMEFinanceSuite.Core.Application.FinancialEntries;

public sealed record UpdateFinancialEntryCommand(
    Guid Id,
    string Description,
    decimal Amount,
    DateOnly OccurredOn,
    EntryType EntryType,
    string? Notes,
    Guid? CustomerId = null,
    Guid? ProductServiceId = null);
