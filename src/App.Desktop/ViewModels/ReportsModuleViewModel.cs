using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.Reports;
using SMEFinanceSuite.Core.Domain.Enums;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class ReportsModuleViewModel : INotifyPropertyChanged
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IFinancialReportsService _financialReportsService;
    private readonly IFinancialReportCsvExporter _financialReportCsvExporter;

    private FinancialReportSummaryDto _summary = FinancialReportSummaryDto.Empty;
    private string _filterFrom = string.Empty;
    private string _filterTo = string.Empty;
    private string _selectedFilterType = "Todos";
    private bool _isBusy;

    public ReportsModuleViewModel(
        IFinancialReportsService financialReportsService,
        IFinancialReportCsvExporter financialReportCsvExporter)
    {
        _financialReportsService = financialReportsService;
        _financialReportCsvExporter = financialReportCsvExporter;
        BreakdownByMonth.CollectionChanged += OnBreakdownCollectionsChanged;
        BreakdownByCustomer.CollectionChanged += OnBreakdownCollectionsChanged;
        BreakdownByProductService.CollectionChanged += OnBreakdownCollectionsChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<FinancialReportMonthlyBreakdownItemViewModel> BreakdownByMonth { get; } = [];

    public ObservableCollection<FinancialReportBreakdownItemViewModel> BreakdownByCustomer { get; } = [];

    public ObservableCollection<FinancialReportBreakdownItemViewModel> BreakdownByProductService { get; } = [];

    public string FilterFrom
    {
        get => _filterFrom;
        set => SetProperty(ref _filterFrom, value);
    }

    public string FilterTo
    {
        get => _filterTo;
        set => SetProperty(ref _filterTo, value);
    }

    public IReadOnlyList<string> FilterTypeOptions { get; } = ["Todos", "Receita", "Despesa"];

    public string SelectedFilterType
    {
        get => _selectedFilterType;
        set => SetProperty(ref _selectedFilterType, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanApplyFilters));
            OnPropertyChanged(nameof(CanClearFilters));
            OnPropertyChanged(nameof(CanExportCsv));
        }
    }

    public bool CanApplyFilters => !IsBusy;

    public bool CanClearFilters => !IsBusy;

    public bool CanExportCsv => !IsBusy;

    public string TotalRevenueDisplay => _summary.TotalRevenue.ToString("C", PortugueseCulture);

    public string TotalExpenseDisplay => _summary.TotalExpense.ToString("C", PortugueseCulture);

    public string NetBalanceDisplay => _summary.NetBalance.ToString("C", PortugueseCulture);

    public bool HasPeriodComparison => _summary.PeriodComparison is not null;

    public string PeriodComparisonDisplay
    {
        get
        {
            FinancialReportPeriodComparisonDto? comparison = _summary.PeriodComparison;

            if (comparison is null)
            {
                return "Comparativo disponível ao definir data inicial e final do período.";
            }

            string currentFrom = comparison.CurrentFrom.ToString("dd/MM/yyyy", PortugueseCulture);
            string currentTo = comparison.CurrentTo.ToString("dd/MM/yyyy", PortugueseCulture);
            string previousFrom = comparison.PreviousFrom.ToString("dd/MM/yyyy", PortugueseCulture);
            string previousTo = comparison.PreviousTo.ToString("dd/MM/yyyy", PortugueseCulture);

            return $"Atual: {currentFrom} até {currentTo} • Anterior: {previousFrom} até {previousTo}";
        }
    }

    public string RevenueComparisonDisplay => BuildComparisonDisplay(
        label: "Receita",
        currentAmount: _summary.TotalRevenue,
        previousAmount: _summary.PeriodComparison?.PreviousTotalRevenue);

    public string ExpenseComparisonDisplay => BuildComparisonDisplay(
        label: "Despesa",
        currentAmount: _summary.TotalExpense,
        previousAmount: _summary.PeriodComparison?.PreviousTotalExpense);

    public string NetBalanceComparisonDisplay => BuildComparisonDisplay(
        label: "Saldo",
        currentAmount: _summary.NetBalance,
        previousAmount: _summary.PeriodComparison?.PreviousNetBalance);

    public string PeriodDisplay
    {
        get
        {
            string from = _summary.From?.ToString("dd/MM/yyyy", PortugueseCulture) ?? "início";
            string to = _summary.To?.ToString("dd/MM/yyyy", PortugueseCulture) ?? "hoje";
            return $"Período analisado: {from} até {to}";
        }
    }

    public string Summary => $"{BreakdownByMonth.Count} meses • {BreakdownByCustomer.Count} clientes • {BreakdownByProductService.Count} itens";

    public void SetBusy(bool isBusy)
    {
        IsBusy = isBusy;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        FinancialReportFilter filter = BuildFilter();

        _summary = await _financialReportsService.GetSummaryAsync(filter, cancellationToken);

        PopulateBreakdowns(_summary);

        OnPropertyChanged(nameof(TotalRevenueDisplay));
        OnPropertyChanged(nameof(TotalExpenseDisplay));
        OnPropertyChanged(nameof(NetBalanceDisplay));
        OnPropertyChanged(nameof(PeriodDisplay));
        OnPropertyChanged(nameof(HasPeriodComparison));
        OnPropertyChanged(nameof(PeriodComparisonDisplay));
        OnPropertyChanged(nameof(RevenueComparisonDisplay));
        OnPropertyChanged(nameof(ExpenseComparisonDisplay));
        OnPropertyChanged(nameof(NetBalanceComparisonDisplay));
    }

    public async Task ApplyFiltersAsync(CancellationToken cancellationToken = default)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
    {
        FilterFrom = string.Empty;
        FilterTo = string.Empty;
        SelectedFilterType = FilterTypeOptions[0];
        await LoadAsync(cancellationToken);
    }

    public FinancialReportCsvExport CreateCsvExport()
    {
        string content = _financialReportCsvExporter.Export(_summary);

        return new FinancialReportCsvExport(
            SuggestedFileName: BuildSuggestedExportFileName(),
            Content: content);
    }

    private void PopulateBreakdowns(FinancialReportSummaryDto summary)
    {
        BreakdownByMonth.Clear();

        foreach (FinancialReportMonthlyBreakdownItemDto item in summary.BreakdownByMonth)
        {
            BreakdownByMonth.Add(new FinancialReportMonthlyBreakdownItemViewModel(
                year: item.Year,
                month: item.Month,
                totalRevenue: item.TotalRevenue,
                totalExpense: item.TotalExpense,
                netBalance: item.NetBalance));
        }

        BreakdownByCustomer.Clear();

        foreach (FinancialReportBreakdownItemDto item in summary.BreakdownByCustomer)
        {
            BreakdownByCustomer.Add(new FinancialReportBreakdownItemViewModel(
                referenceId: item.ReferenceId,
                label: item.Label,
                category: item.Category,
                totalRevenue: item.TotalRevenue,
                totalExpense: item.TotalExpense,
                netBalance: item.NetBalance));
        }

        BreakdownByProductService.Clear();

        foreach (FinancialReportBreakdownItemDto item in summary.BreakdownByProductService)
        {
            BreakdownByProductService.Add(new FinancialReportBreakdownItemViewModel(
                referenceId: item.ReferenceId,
                label: item.Label,
                category: item.Category,
                totalRevenue: item.TotalRevenue,
                totalExpense: item.TotalExpense,
                netBalance: item.NetBalance));
        }
    }

    private FinancialReportFilter BuildFilter()
    {
        DateOnly? from = DateOnlyInputParser.ParseOptional(FilterFrom, "Data inicial");
        DateOnly? to = DateOnlyInputParser.ParseOptional(FilterTo, "Data final");
        EntryType? entryType = SelectedFilterType switch
        {
            "Receita" => EntryType.Revenue,
            "Despesa" => EntryType.Expense,
            _ => null
        };

        return new FinancialReportFilter(From: from, To: to, EntryType: entryType);
    }

    private string BuildSuggestedExportFileName()
    {
        string from = _summary.From?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "inicio";
        string to = _summary.To?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "hoje";
        return $"relatorio-financeiro-{from}-a-{to}.csv";
    }

    private string BuildComparisonDisplay(string label, decimal currentAmount, decimal? previousAmount)
    {
        if (previousAmount is null)
        {
            return $"{label}: comparativo indisponível";
        }

        decimal previous = previousAmount.Value;
        decimal variation = currentAmount - previous;
        decimal? variationPercent = previous == 0m
            ? null
            : variation / Math.Abs(previous);

        string currentDisplay = currentAmount.ToString("C", PortugueseCulture);
        string previousDisplay = previous.ToString("C", PortugueseCulture);
        string variationDisplay = FormatSignedCurrency(variation);
        string variationPercentDisplay = variationPercent is null
            ? "n/d"
            : variationPercent.Value.ToString("+0.00%;-0.00%;0.00%", PortugueseCulture);

        return $"{label}: atual {currentDisplay} | anterior {previousDisplay} | variação {variationDisplay} ({variationPercentDisplay})";
    }

    private string FormatSignedCurrency(decimal value)
    {
        if (value == 0m)
        {
            return value.ToString("C", PortugueseCulture);
        }

        string absoluteValueDisplay = Math.Abs(value).ToString("C", PortugueseCulture);
        return value > 0m ? $"+{absoluteValueDisplay}" : $"-{absoluteValueDisplay}";
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnBreakdownCollectionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Summary));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
