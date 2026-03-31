using System.Globalization;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class FinancialReportBreakdownItemViewModel
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");

    public FinancialReportBreakdownItemViewModel(
        Guid? referenceId,
        string label,
        string? category,
        decimal totalRevenue,
        decimal totalExpense,
        decimal netBalance)
    {
        ReferenceId = referenceId;
        Label = label;
        Category = category;
        TotalRevenue = totalRevenue;
        TotalExpense = totalExpense;
        NetBalance = netBalance;
    }

    public Guid? ReferenceId { get; }

    public string Label { get; }

    public string? Category { get; }

    public decimal TotalRevenue { get; }

    public decimal TotalExpense { get; }

    public decimal NetBalance { get; }

    public string CategoryDisplay => string.IsNullOrWhiteSpace(Category) ? "-" : Category;

    public string TotalRevenueDisplay => TotalRevenue.ToString("C", PortugueseCulture);

    public string TotalExpenseDisplay => TotalExpense.ToString("C", PortugueseCulture);

    public string NetBalanceDisplay => NetBalance.ToString("C", PortugueseCulture);
}
