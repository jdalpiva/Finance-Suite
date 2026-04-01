using SMEFinanceSuite.Core.Domain.Enums;

namespace SMEFinanceSuite.Core.Application.Reports;

public sealed record FinancialReportFilter(
    DateOnly? From = null,
    DateOnly? To = null,
    EntryType? EntryType = null);
