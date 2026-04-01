namespace SMEFinanceSuite.Core.Application.Reports;

public sealed record FinancialReportPeriodComparisonDto(
    DateOnly CurrentFrom,
    DateOnly CurrentTo,
    DateOnly PreviousFrom,
    DateOnly PreviousTo,
    decimal PreviousTotalRevenue,
    decimal PreviousTotalExpense,
    decimal PreviousNetBalance);
