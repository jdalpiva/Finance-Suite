using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.Customers;
using SMEFinanceSuite.Core.Application.Dashboard;
using SMEFinanceSuite.Core.Application.FinancialEntries;
using SMEFinanceSuite.Core.Domain.Enums;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IFinancialDashboardService _financialDashboardService;
    private readonly IFinancialEntryService _financialEntryService;
    private readonly ICustomerService _customerService;

    private DashboardSummaryDto _summary = DashboardSummaryDto.Empty;
    private string _statusMessage = "Preparando dashboard e lançamentos...";

    private string _filterFrom = string.Empty;
    private string _filterTo = string.Empty;
    private string _selectedFilterType = "Todos";
    private string _newCustomerName = string.Empty;

    private FinancialEntryListItemViewModel? _selectedFinancialEntry;
    private bool _isBusy;
    private bool _isDeleteConfirmationPending;
    private Guid? _deleteConfirmationEntryId;

    public MainWindowViewModel(
        IFinancialDashboardService financialDashboardService,
        IFinancialEntryService financialEntryService,
        ICustomerService customerService)
    {
        _financialDashboardService = financialDashboardService;
        _financialEntryService = financialEntryService;
        _customerService = customerService;

        FinancialEntries.CollectionChanged += OnFinancialEntriesChanged;
        Customers.CollectionChanged += OnCustomersChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public FinancialEntryFormViewModel EntryForm { get; } = new();

    public ObservableCollection<FinancialEntryListItemViewModel> FinancialEntries { get; } = [];

    public ObservableCollection<CustomerListItemViewModel> Customers { get; } = [];

    public IReadOnlyList<string> FilterTypeOptions { get; } = ["Todos", "Receita", "Despesa"];

    public string TotalRevenueDisplay => _summary.TotalRevenue.ToString("C", PortugueseCulture);

    public string TotalExpenseDisplay => _summary.TotalExpense.ToString("C", PortugueseCulture);

    public string NetCashFlowDisplay => _summary.NetCashFlow.ToString("C", PortugueseCulture);

    public string RegistrySummary => $"{_summary.CustomersCount} clientes • {_summary.ProductsCount} itens";

    public string EntriesSummary => $"{FinancialEntries.Count} lançamentos";

    public string CustomersSummary => $"{Customers.Count} clientes cadastrados";

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
            OnPropertyChanged(nameof(CanRegisterCustomer));
        }
    }

    public bool IsDeleteConfirmationPending => _isDeleteConfirmationPending;

    public string DeleteSelectedEntryButtonLabel => IsDeleteConfirmationPending ? "Confirmar exclusão" : "Excluir selecionado";

    public bool CanApplyFilters => !IsBusy;

    public bool CanClearFilters => !IsBusy;

    public bool CanRegisterEntry => !IsBusy;

    public bool CanUpdateSelectedEntry => !IsBusy && SelectedFinancialEntry is not null;

    public bool CanDeleteSelectedEntry => !IsBusy && SelectedFinancialEntry is not null;

    public bool CanRegisterCustomer => !IsBusy;

    public FinancialEntryListItemViewModel? SelectedFinancialEntry
    {
        get => _selectedFinancialEntry;
        set
        {
            if (!SetProperty(ref _selectedFinancialEntry, value))
            {
                return;
            }

            ResetDeleteConfirmation();
            OnPropertyChanged(nameof(CanUpdateSelectedEntry));
            OnPropertyChanged(nameof(CanDeleteSelectedEntry));

            if (value is not null)
            {
                EntryForm.FillFromSelection(value);
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

    public string NewCustomerName
    {
        get => _newCustomerName;
        set => SetProperty(ref _newCustomerName, value);
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
            await LoadCustomersAsync(cancellationToken);
            StatusMessage = $"Dashboard e módulos carregados em {DateTime.Now:dd/MM/yyyy HH:mm}.";
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
            ResetDeleteConfirmation();
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

            ResetDeleteConfirmation();
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
            CreateFinancialEntryCommand command = EntryForm.BuildCreateCommand();
            await _financialEntryService.RegisterAsync(command, cancellationToken);

            SelectedFinancialEntry = null;
            EntryForm.Clear();
            ResetDeleteConfirmation();
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

            UpdateFinancialEntryCommand command = EntryForm.BuildUpdateCommand(SelectedFinancialEntry.Id);
            await _financialEntryService.UpdateAsync(command, cancellationToken);

            ResetDeleteConfirmation();
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
        if (IsBusy)
        {
            return;
        }

        if (SelectedFinancialEntry is null)
        {
            StatusMessage = "Selecione um lançamento para excluir.";
            return;
        }

        if (!IsDeleteConfirmationPending || _deleteConfirmationEntryId != SelectedFinancialEntry.Id)
        {
            SetDeleteConfirmation(SelectedFinancialEntry.Id);
            StatusMessage = "Confirme a exclusão clicando novamente no botão.";
            return;
        }

        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            Guid selectedEntryId = SelectedFinancialEntry.Id;
            await _financialEntryService.DeleteAsync(selectedEntryId, cancellationToken);

            SelectedFinancialEntry = null;
            EntryForm.Clear();
            ResetDeleteConfirmation();
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

    public async Task RegisterCustomerAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            string customerName = NewCustomerName.Trim();

            if (customerName.Length == 0)
            {
                throw new InvalidOperationException("Informe o nome do cliente.");
            }

            await _customerService.RegisterAsync(new CreateCustomerCommand(Name: customerName), cancellationToken);

            NewCustomerName = string.Empty;
            await LoadCustomersAsync(cancellationToken);
            await LoadDashboardAsync(cancellationToken);

            StatusMessage = $"Cliente cadastrado em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao cadastrar cliente: {exception.Message}";
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

    private async Task LoadCustomersAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CustomerListItemDto> customers = await _customerService.ListAsync(cancellationToken);

        Customers.Clear();

        foreach (CustomerListItemDto customer in customers)
        {
            Customers.Add(new CustomerListItemViewModel(
                id: customer.Id,
                name: customer.Name));
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

    private void SetDeleteConfirmation(Guid entryId)
    {
        _isDeleteConfirmationPending = true;
        _deleteConfirmationEntryId = entryId;
        OnPropertyChanged(nameof(IsDeleteConfirmationPending));
        OnPropertyChanged(nameof(DeleteSelectedEntryButtonLabel));
    }

    private void ResetDeleteConfirmation()
    {
        if (!_isDeleteConfirmationPending && _deleteConfirmationEntryId is null)
        {
            return;
        }

        _isDeleteConfirmationPending = false;
        _deleteConfirmationEntryId = null;
        OnPropertyChanged(nameof(IsDeleteConfirmationPending));
        OnPropertyChanged(nameof(DeleteSelectedEntryButtonLabel));
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

    private void OnCustomersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(CustomersSummary));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
