using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.Reports;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class ReportsModuleViewModel : INotifyPropertyChanged
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IFinancialReportsService _financialReportsService;

    private FinancialReportSummaryDto _summary = FinancialReportSummaryDto.Empty;
    private string _filterFrom = string.Empty;
    private string _filterTo = string.Empty;
    private bool _isBusy;

    public ReportsModuleViewModel(IFinancialReportsService financialReportsService)
    {
        _financialReportsService = financialReportsService;
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
        }
    }

    public bool CanApplyFilters => !IsBusy;

    public bool CanClearFilters => !IsBusy;

    public string TotalRevenueDisplay => _summary.TotalRevenue.ToString("C", PortugueseCulture);

    public string TotalExpenseDisplay => _summary.TotalExpense.ToString("C", PortugueseCulture);

    public string NetBalanceDisplay => _summary.NetBalance.ToString("C", PortugueseCulture);

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
    }

    public async Task ApplyFiltersAsync(CancellationToken cancellationToken = default)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
    {
        FilterFrom = string.Empty;
        FilterTo = string.Empty;
        await LoadAsync(cancellationToken);
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

        return new FinancialReportFilter(From: from, To: to);
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
