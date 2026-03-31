namespace SMEFinanceSuite.Core.Application.Reports;

public sealed record FinancialReportBreakdownItemDto(
    Guid? ReferenceId,
    string Label,
    string? Category,
    decimal TotalRevenue,
    decimal TotalExpense,
    decimal NetBalance);
