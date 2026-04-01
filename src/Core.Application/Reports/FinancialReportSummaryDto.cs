namespace SMEFinanceSuite.Core.Application.Reports;

public sealed record FinancialReportSummaryDto(
    DateOnly? From,
    DateOnly? To,
    decimal TotalRevenue,
    decimal TotalExpense,
    decimal NetBalance,
    IReadOnlyList<FinancialReportMonthlyBreakdownItemDto> BreakdownByMonth,
    IReadOnlyList<FinancialReportBreakdownItemDto> BreakdownByCustomer,
    IReadOnlyList<FinancialReportBreakdownItemDto> BreakdownByProductService,
    FinancialReportPeriodComparisonDto? PeriodComparison = null)
{
    public static FinancialReportSummaryDto Empty { get; } = new(
        From: null,
        To: null,
        TotalRevenue: 0m,
        TotalExpense: 0m,
        NetBalance: 0m,
        BreakdownByMonth: [],
        BreakdownByCustomer: [],
        BreakdownByProductService: [],
        PeriodComparison: null);
}
