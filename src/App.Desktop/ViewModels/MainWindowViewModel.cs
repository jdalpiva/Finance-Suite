using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.Dashboard;
using SMEFinanceSuite.Core.Application.FinancialEntries;
using SMEFinanceSuite.Core.Domain.Enums;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IFinancialDashboardService _financialDashboardService;
    private readonly IFinancialEntryService _financialEntryService;
    private DashboardSummaryDto _summary = DashboardSummaryDto.Empty;
    private string _statusMessage = "Preparando dashboard e lançamentos...";

    private string _filterFrom = string.Empty;
    private string _filterTo = string.Empty;
    private string _selectedFilterType = "Todos";

    private string _newEntryDescription = string.Empty;
    private string _newEntryAmount = string.Empty;
    private string _newEntryOccurredOn = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private string _selectedFormEntryType = "Receita";
    private string _newEntryNotes = string.Empty;

    public MainWindowViewModel(
        IFinancialDashboardService financialDashboardService,
        IFinancialEntryService financialEntryService)
    {
        _financialDashboardService = financialDashboardService;
        _financialEntryService = financialEntryService;

        FinancialEntries.CollectionChanged += OnFinancialEntriesChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<FinancialEntryListItemViewModel> FinancialEntries { get; } = [];

    public IReadOnlyList<string> FilterTypeOptions { get; } = ["Todos", "Receita", "Despesa"];

    public IReadOnlyList<string> FormEntryTypeOptions { get; } = ["Receita", "Despesa"];

    public string TotalRevenueDisplay => _summary.TotalRevenue.ToString("C", PortugueseCulture);

    public string TotalExpenseDisplay => _summary.TotalExpense.ToString("C", PortugueseCulture);

    public string NetCashFlowDisplay => _summary.NetCashFlow.ToString("C", PortugueseCulture);

    public string RegistrySummary => $"{_summary.CustomersCount} clientes • {_summary.ProductsCount} itens";

    public string EntriesSummary => $"{FinancialEntries.Count} lançamentos";

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

    public string SelectedFilterType
    {
        get => _selectedFilterType;
        set => SetProperty(ref _selectedFilterType, value);
    }

    public string NewEntryDescription
    {
        get => _newEntryDescription;
        set => SetProperty(ref _newEntryDescription, value);
    }

    public string NewEntryAmount
    {
        get => _newEntryAmount;
        set => SetProperty(ref _newEntryAmount, value);
    }

    public string NewEntryOccurredOn
    {
        get => _newEntryOccurredOn;
        set => SetProperty(ref _newEntryOccurredOn, value);
    }

    public string SelectedFormEntryType
    {
        get => _selectedFormEntryType;
        set => SetProperty(ref _selectedFormEntryType, value);
    }

    public string NewEntryNotes
    {
        get => _newEntryNotes;
        set => SetProperty(ref _newEntryNotes, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await LoadDashboardAsync(cancellationToken);
            await LoadEntriesAsync(cancellationToken);
            StatusMessage = $"Dashboard e lançamentos carregados em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao carregar dados iniciais: {exception.Message}";
        }
    }

    public async Task ApplyFiltersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await LoadEntriesAsync(cancellationToken);
            StatusMessage = $"Filtros aplicados em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao aplicar filtros: {exception.Message}";
        }
    }

    public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
    {
        FilterFrom = string.Empty;
        FilterTo = string.Empty;
        SelectedFilterType = FilterTypeOptions[0];

        await ApplyFiltersAsync(cancellationToken);
    }

    public async Task RegisterEntryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            CreateFinancialEntryCommand command = BuildCreateCommand();
            await _financialEntryService.RegisterAsync(command, cancellationToken);

            ClearEntryForm();
            await LoadEntriesAsync(cancellationToken);
            await LoadDashboardAsync(cancellationToken);

            StatusMessage = $"Lançamento cadastrado em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao cadastrar lançamento: {exception.Message}";
        }
    }

    private async Task LoadDashboardAsync(CancellationToken cancellationToken)
    {
        _summary = await _financialDashboardService.GetSummaryAsync(cancellationToken: cancellationToken);

        OnPropertyChanged(nameof(TotalRevenueDisplay));
        OnPropertyChanged(nameof(TotalExpenseDisplay));
        OnPropertyChanged(nameof(NetCashFlowDisplay));
        OnPropertyChanged(nameof(RegistrySummary));
    }

    private async Task LoadEntriesAsync(CancellationToken cancellationToken)
    {
        FinancialEntriesFilter filter = BuildFilter();

        IReadOnlyList<FinancialEntryListItemDto> entries = await _financialEntryService.ListAsync(filter, cancellationToken);

        FinancialEntries.Clear();

        foreach (FinancialEntryListItemDto entry in entries)
        {
            FinancialEntries.Add(new FinancialEntryListItemViewModel(
                description: entry.Description,
                occurredOnDisplay: entry.OccurredOn.ToString("dd/MM/yyyy", PortugueseCulture),
                amountDisplay: entry.Amount.ToString("C", PortugueseCulture),
                entryTypeDisplay: ToEntryTypeLabel(entry.EntryType),
                notesDisplay: string.IsNullOrWhiteSpace(entry.Notes) ? "-" : entry.Notes));
        }
    }

    private FinancialEntriesFilter BuildFilter()
    {
        DateOnly? from = ParseOptionalDate(FilterFrom, "Data inicial");
        DateOnly? to = ParseOptionalDate(FilterTo, "Data final");

        EntryType? entryType = SelectedFilterType switch
        {
            "Receita" => EntryType.Revenue,
            "Despesa" => EntryType.Expense,
            _ => null
        };

        return new FinancialEntriesFilter(From: from, To: to, EntryType: entryType);
    }

    private CreateFinancialEntryCommand BuildCreateCommand()
    {
        string description = NewEntryDescription.Trim();

        if (description.Length == 0)
        {
            throw new InvalidOperationException("Informe a descrição do lançamento.");
        }

        if (!TryParseAmount(NewEntryAmount, out decimal amount))
        {
            throw new InvalidOperationException("Informe um valor válido. Exemplo: 1500,50");
        }

        DateOnly occurredOn = ParseRequiredDate(NewEntryOccurredOn, "Data do lançamento");

        EntryType entryType = SelectedFormEntryType == "Despesa"
            ? EntryType.Expense
            : EntryType.Revenue;

        string? notes = string.IsNullOrWhiteSpace(NewEntryNotes)
            ? null
            : NewEntryNotes.Trim();

        return new CreateFinancialEntryCommand(
            Description: description,
            Amount: amount,
            OccurredOn: occurredOn,
            EntryType: entryType,
            Notes: notes);
    }

    private void ClearEntryForm()
    {
        NewEntryDescription = string.Empty;
        NewEntryAmount = string.Empty;
        NewEntryOccurredOn = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        SelectedFormEntryType = FormEntryTypeOptions[0];
        NewEntryNotes = string.Empty;
    }

    private static bool TryParseAmount(string rawAmount, out decimal amount)
    {
        if (decimal.TryParse(rawAmount, NumberStyles.Number, PortugueseCulture, out amount))
        {
            return true;
        }

        return decimal.TryParse(rawAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    private static DateOnly ParseRequiredDate(string rawDate, string fieldName)
    {
        if (TryParseDate(rawDate, out DateOnly parsedDate))
        {
            return parsedDate;
        }

        throw new InvalidOperationException($"{fieldName} inválida. Use yyyy-MM-dd.");
    }

    private static DateOnly? ParseOptionalDate(string rawDate, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(rawDate))
        {
            return null;
        }

        if (TryParseDate(rawDate, out DateOnly parsedDate))
        {
            return parsedDate;
        }

        throw new InvalidOperationException($"{fieldName} inválida. Use yyyy-MM-dd.");
    }

    private static bool TryParseDate(string rawDate, out DateOnly parsedDate)
    {
        if (DateOnly.TryParseExact(rawDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
        {
            return true;
        }

        if (DateOnly.TryParse(rawDate, PortugueseCulture, DateTimeStyles.None, out parsedDate))
        {
            return true;
        }

        return DateOnly.TryParse(rawDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);
    }

    private static string ToEntryTypeLabel(EntryType entryType)
    {
        return entryType == EntryType.Revenue ? "Receita" : "Despesa";
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

    private void OnFinancialEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(EntriesSummary));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
