namespace SMEFinanceSuite.Core.Application.Dashboard;

public sealed record DashboardSummaryDto(
    decimal TotalRevenue,
    decimal TotalExpense,
    decimal NetCashFlow,
    int CustomersCount,
    int ProductsCount)
{
    public static DashboardSummaryDto Empty { get; } = new(
        TotalRevenue: 0m,
        TotalExpense: 0m,
        NetCashFlow: 0m,
        CustomersCount: 0,
        ProductsCount: 0);
}
