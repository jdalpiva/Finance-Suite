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

    private FinancialEntryListItemViewModel? _selectedFinancialEntry;
    private bool _isBusy;

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
            OnPropertyChanged(nameof(CanRegisterEntry));
            OnPropertyChanged(nameof(CanUpdateSelectedEntry));
            OnPropertyChanged(nameof(CanDeleteSelectedEntry));
        }
    }

    public bool CanApplyFilters => !IsBusy;

    public bool CanClearFilters => !IsBusy;

    public bool CanRegisterEntry => !IsBusy;

    public bool CanUpdateSelectedEntry => !IsBusy && SelectedFinancialEntry is not null;

    public bool CanDeleteSelectedEntry => !IsBusy && SelectedFinancialEntry is not null;

    public FinancialEntryListItemViewModel? SelectedFinancialEntry
    {
        get => _selectedFinancialEntry;
        set
        {
            if (!SetProperty(ref _selectedFinancialEntry, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanUpdateSelectedEntry));
            OnPropertyChanged(nameof(CanDeleteSelectedEntry));

            if (value is not null)
            {
                FillEntryFormFromSelection(value);
            }
        }
    }

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
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            await LoadEntriesAsync(cancellationToken);
            StatusMessage = $"Filtros aplicados em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao aplicar filtros: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            FilterFrom = string.Empty;
            FilterTo = string.Empty;
            SelectedFilterType = FilterTypeOptions[0];

            await LoadEntriesAsync(cancellationToken);
            StatusMessage = $"Filtros limpos em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao limpar filtros: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task RegisterEntryAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            CreateFinancialEntryCommand command = BuildCreateCommand();
            await _financialEntryService.RegisterAsync(command, cancellationToken);

            SelectedFinancialEntry = null;
            ClearEntryForm();
            await LoadEntriesAsync(cancellationToken);
            await LoadDashboardAsync(cancellationToken);

            StatusMessage = $"Lançamento cadastrado em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao cadastrar lançamento: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task UpdateSelectedEntryAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            if (SelectedFinancialEntry is null)
            {
                throw new InvalidOperationException("Selecione um lançamento para atualizar.");
            }

            UpdateFinancialEntryCommand command = BuildUpdateCommand(SelectedFinancialEntry.Id);
            await _financialEntryService.UpdateAsync(command, cancellationToken);

            await LoadEntriesAsync(cancellationToken, selectedEntryId: command.Id);
            await LoadDashboardAsync(cancellationToken);

            StatusMessage = $"Lançamento atualizado em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao atualizar lançamento: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task DeleteSelectedEntryAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            if (SelectedFinancialEntry is null)
            {
                throw new InvalidOperationException("Selecione um lançamento para excluir.");
            }

            Guid selectedEntryId = SelectedFinancialEntry.Id;
            await _financialEntryService.DeleteAsync(selectedEntryId, cancellationToken);

            SelectedFinancialEntry = null;
            ClearEntryForm();
            await LoadEntriesAsync(cancellationToken);
            await LoadDashboardAsync(cancellationToken);

            StatusMessage = $"Lançamento excluído em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao excluir lançamento: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
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

    private async Task LoadEntriesAsync(CancellationToken cancellationToken, Guid? selectedEntryId = null)
    {
        FinancialEntriesFilter filter = BuildFilter();

        IReadOnlyList<FinancialEntryListItemDto> entries = await _financialEntryService.ListAsync(filter, cancellationToken);

        Guid? selectedEntryIdToRestore = selectedEntryId ?? SelectedFinancialEntry?.Id;

        FinancialEntries.Clear();

        foreach (FinancialEntryListItemDto entry in entries)
        {
            FinancialEntries.Add(new FinancialEntryListItemViewModel(
                id: entry.Id,
                description: entry.Description,
                amount: entry.Amount,
                occurredOn: entry.OccurredOn,
                occurredOnDisplay: entry.OccurredOn.ToString("dd/MM/yyyy", PortugueseCulture),
                amountDisplay: entry.Amount.ToString("C", PortugueseCulture),
                entryTypeDisplay: ToEntryTypeLabel(entry.EntryType),
                notes: entry.Notes,
                notesDisplay: string.IsNullOrWhiteSpace(entry.Notes) ? "-" : entry.Notes));
        }

        if (selectedEntryIdToRestore is null)
        {
            SelectedFinancialEntry = null;
            return;
        }

        SelectedFinancialEntry = FinancialEntries.FirstOrDefault(entry => entry.Id == selectedEntryIdToRestore.Value);
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
        (string description, decimal amount, DateOnly occurredOn, EntryType entryType, string? notes) = BuildEntryInput();

        return new CreateFinancialEntryCommand(
            Description: description,
            Amount: amount,
            OccurredOn: occurredOn,
            EntryType: entryType,
            Notes: notes);
    }

    private UpdateFinancialEntryCommand BuildUpdateCommand(Guid id)
    {
        (string description, decimal amount, DateOnly occurredOn, EntryType entryType, string? notes) = BuildEntryInput();

        return new UpdateFinancialEntryCommand(
            Id: id,
            Description: description,
            Amount: amount,
            OccurredOn: occurredOn,
            EntryType: entryType,
            Notes: notes);
    }

    private (string Description, decimal Amount, DateOnly OccurredOn, EntryType EntryType, string? Notes) BuildEntryInput()
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

        return (description, amount, occurredOn, entryType, notes);
    }

    private void FillEntryFormFromSelection(FinancialEntryListItemViewModel selectedEntry)
    {
        NewEntryDescription = selectedEntry.Description;
        NewEntryAmount = selectedEntry.Amount.ToString("0.00", PortugueseCulture);
        NewEntryOccurredOn = selectedEntry.OccurredOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        SelectedFormEntryType = selectedEntry.EntryTypeDisplay;
        NewEntryNotes = selectedEntry.Notes ?? string.Empty;
    }

    private void ClearEntryForm()
    {
        NewEntryDescription = string.Empty;
        NewEntryAmount = string.Empty;
        NewEntryOccurredOn = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        SelectedFormEntryType = FormEntryTypeOptions[0];
        NewEntryNotes = string.Empty;
    }

    private bool TryBeginBusyOperation()
    {
        if (IsBusy)
        {
            return false;
        }

        IsBusy = true;
        return true;
    }

    private void EndBusyOperation()
    {
        IsBusy = false;
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
