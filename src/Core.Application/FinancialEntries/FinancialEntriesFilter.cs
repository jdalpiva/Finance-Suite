using SMEFinanceSuite.Core.Domain.Enums;

namespace SMEFinanceSuite.Core.Application.FinancialEntries;

public sealed record FinancialEntriesFilter(
    DateOnly? From = null,
    DateOnly? To = null,
    EntryType? EntryType = null,
    int MaxResults = 200);
