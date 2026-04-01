namespace SMEFinanceSuite.Core.Application.Reports;

public sealed record FinancialReportMonthlyBreakdownItemDto(
    int Year,
    int Month,
    decimal TotalRevenue,
    decimal TotalExpense,
    decimal NetBalance);
