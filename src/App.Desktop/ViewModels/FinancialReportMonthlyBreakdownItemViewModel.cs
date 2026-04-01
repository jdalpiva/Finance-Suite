using System.Globalization;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class FinancialReportMonthlyBreakdownItemViewModel
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");

    public FinancialReportMonthlyBreakdownItemViewModel(
        int year,
        int month,
        decimal totalRevenue,
        decimal totalExpense,
        decimal netBalance)
    {
        Year = year;
        Month = month;
        TotalRevenue = totalRevenue;
        TotalExpense = totalExpense;
        NetBalance = netBalance;
    }

    public int Year { get; }

    public int Month { get; }

    public decimal TotalRevenue { get; }

    public decimal TotalExpense { get; }

    public decimal NetBalance { get; }

    public string MonthDisplay => new DateOnly(Year, Month, 1).ToString("MM/yyyy", PortugueseCulture);

    public string TotalRevenueDisplay => TotalRevenue.ToString("C", PortugueseCulture);

    public string TotalExpenseDisplay => TotalExpense.ToString("C", PortugueseCulture);

    public string NetBalanceDisplay => NetBalance.ToString("C", PortugueseCulture);
}
