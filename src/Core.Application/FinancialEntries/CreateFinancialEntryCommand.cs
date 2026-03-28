using SMEFinanceSuite.Core.Domain.Enums;

namespace SMEFinanceSuite.Core.Application.FinancialEntries;

public sealed record CreateFinancialEntryCommand(
    string Description,
    decimal Amount,
    DateOnly OccurredOn,
    EntryType EntryType,
    Guid? CustomerId = null,
    Guid? ProductServiceId = null,
    string? Notes = null);
