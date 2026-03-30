using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.Customers;
using SMEFinanceSuite.Core.Application.Dashboard;
using SMEFinanceSuite.Core.Application.FinancialEntries;
using SMEFinanceSuite.Core.Application.ProductServices;
using SMEFinanceSuite.Core.Domain.Enums;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IFinancialDashboardService _financialDashboardService;
    private readonly IFinancialEntryService _financialEntryService;
    private readonly ICustomerService _customerService;
    private readonly IProductCatalogService _productCatalogService;

    private DashboardSummaryDto _summary = DashboardSummaryDto.Empty;
    private string _statusMessage = "Preparando dashboard e módulos...";

    private string _filterFrom = string.Empty;
    private string _filterTo = string.Empty;
    private string _selectedFilterType = "Todos";

    private FinancialEntryListItemViewModel? _selectedFinancialEntry;
    private CustomerListItemViewModel? _selectedCustomer;
    private bool _isBusy;
    private bool _isDeleteConfirmationPending;
    private Guid? _deleteConfirmationEntryId;
    private bool _isCustomerDeleteConfirmationPending;
    private Guid? _deleteConfirmationCustomerId;

    public MainWindowViewModel(
        IFinancialDashboardService financialDashboardService,
        IFinancialEntryService financialEntryService,
        ICustomerService customerService,
        IProductCatalogService productCatalogService)
    {
        _financialDashboardService = financialDashboardService;
        _financialEntryService = financialEntryService;
        _customerService = customerService;
        _productCatalogService = productCatalogService;

        FinancialEntries.CollectionChanged += OnFinancialEntriesChanged;
        Customers.CollectionChanged += OnCustomersChanged;
        ProductServices.CollectionChanged += OnProductServicesChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public FinancialEntryFormViewModel EntryForm { get; } = new();

    public CustomerFormViewModel CustomerForm { get; } = new();

    public ProductServiceFormViewModel ProductServiceForm { get; } = new();

    public ObservableCollection<FinancialEntryListItemViewModel> FinancialEntries { get; } = [];

    public ObservableCollection<CustomerListItemViewModel> Customers { get; } = [];

    public ObservableCollection<ProductServiceListItemViewModel> ProductServices { get; } = [];

    public IReadOnlyList<string> FilterTypeOptions { get; } = ["Todos", "Receita", "Despesa"];

    public string TotalRevenueDisplay => _summary.TotalRevenue.ToString("C", PortugueseCulture);

    public string TotalExpenseDisplay => _summary.TotalExpense.ToString("C", PortugueseCulture);

    public string NetCashFlowDisplay => _summary.NetCashFlow.ToString("C", PortugueseCulture);

    public string RegistrySummary => $"{_summary.CustomersCount} clientes • {_summary.ProductsCount} itens";

    public string EntriesSummary => $"{FinancialEntries.Count} lançamentos";

    public string CustomersSummary => $"{Customers.Count} clientes cadastrados";

    public string ProductServicesSummary => $"{ProductServices.Count} itens cadastrados";

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
            OnPropertyChanged(nameof(CanUpdateSelectedCustomer));
            OnPropertyChanged(nameof(CanDeleteSelectedCustomer));
            OnPropertyChanged(nameof(CanRegisterProductService));
        }
    }

    public bool IsDeleteConfirmationPending => _isDeleteConfirmationPending;

    public string DeleteSelectedEntryButtonLabel => IsDeleteConfirmationPending ? "Confirmar exclusão" : "Excluir selecionado";

    public bool IsCustomerDeleteConfirmationPending => _isCustomerDeleteConfirmationPending;

    public string DeleteSelectedCustomerButtonLabel => IsCustomerDeleteConfirmationPending ? "Confirmar exclusão" : "Excluir selecionado";

    public bool CanApplyFilters => !IsBusy;

    public bool CanClearFilters => !IsBusy;

    public bool CanRegisterEntry => !IsBusy;

    public bool CanUpdateSelectedEntry => !IsBusy && SelectedFinancialEntry is not null;

    public bool CanDeleteSelectedEntry => !IsBusy && SelectedFinancialEntry is not null;

    public bool CanRegisterCustomer => !IsBusy;

    public bool CanUpdateSelectedCustomer => !IsBusy && SelectedCustomer is not null;

    public bool CanDeleteSelectedCustomer => !IsBusy && SelectedCustomer is not null;

    public bool CanRegisterProductService => !IsBusy;

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

    public CustomerListItemViewModel? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (!SetProperty(ref _selectedCustomer, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanUpdateSelectedCustomer));
            OnPropertyChanged(nameof(CanDeleteSelectedCustomer));
            ResetCustomerDeleteConfirmation();

            if (value is null)
            {
                CustomerForm.Clear();
                return;
            }

            CustomerForm.FillFromSelection(value);
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
            await LoadProductServicesAsync(cancellationToken);
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
            CreateCustomerCommand command = CustomerForm.BuildCreateCommand();
            await _customerService.RegisterAsync(command, cancellationToken);

            SelectedCustomer = null;
            ResetCustomerDeleteConfirmation();
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

    public async Task UpdateSelectedCustomerAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            if (SelectedCustomer is null)
            {
                throw new InvalidOperationException("Selecione um cliente para atualizar.");
            }

            UpdateCustomerCommand command = CustomerForm.BuildUpdateCommand(SelectedCustomer.Id);
            await _customerService.UpdateAsync(command, cancellationToken);

            ResetCustomerDeleteConfirmation();
            await LoadCustomersAsync(cancellationToken, selectedCustomerId: command.Id);
            await LoadDashboardAsync(cancellationToken);

            StatusMessage = $"Cliente atualizado em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao atualizar cliente: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task DeleteSelectedCustomerAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        if (SelectedCustomer is null)
        {
            StatusMessage = "Selecione um cliente para excluir.";
            return;
        }

        if (!IsCustomerDeleteConfirmationPending || _deleteConfirmationCustomerId != SelectedCustomer.Id)
        {
            SetCustomerDeleteConfirmation(SelectedCustomer.Id);
            StatusMessage = "Confirme a exclusão do cliente clicando novamente no botão.";
            return;
        }

        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            Guid customerId = SelectedCustomer.Id;
            await _customerService.DeleteAsync(customerId, cancellationToken);

            if (EntryForm.SelectedCustomerId == customerId)
            {
                EntryForm.SelectedCustomerId = null;
            }

            SelectedCustomer = null;
            ResetCustomerDeleteConfirmation();
            await LoadCustomersAsync(cancellationToken);
            await LoadDashboardAsync(cancellationToken);

            StatusMessage = $"Cliente excluído em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao excluir cliente: {exception.Message}";
        }
        finally
        {
            EndBusyOperation();
        }
    }

    public async Task RegisterProductServiceAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginBusyOperation())
        {
            return;
        }

        try
        {
            CreateProductServiceCommand command = ProductServiceForm.BuildCreateCommand();
            await _productCatalogService.RegisterAsync(command, cancellationToken);

            ProductServiceForm.Clear();
            await LoadProductServicesAsync(cancellationToken);
            await LoadDashboardAsync(cancellationToken);

            StatusMessage = $"Produto/serviço cadastrado em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Falha ao cadastrar produto/serviço: {exception.Message}";
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
                customerId: entry.CustomerId,
                productServiceId: entry.ProductServiceId,
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

    private async Task LoadCustomersAsync(CancellationToken cancellationToken, Guid? selectedCustomerId = null)
    {
        IReadOnlyList<CustomerListItemDto> customers = await _customerService.ListAsync(cancellationToken);

        Guid? selectedCustomerIdToRestore = selectedCustomerId ?? SelectedCustomer?.Id;

        Customers.Clear();

        foreach (CustomerListItemDto customer in customers)
        {
            Customers.Add(new CustomerListItemViewModel(
                id: customer.Id,
                name: customer.Name,
                email: customer.Email,
                phone: customer.Phone));
        }

        if (selectedCustomerIdToRestore is null)
        {
            SelectedCustomer = null;
        }
        else
        {
            SelectedCustomer = Customers.FirstOrDefault(customer => customer.Id == selectedCustomerIdToRestore.Value);
        }

        if (EntryForm.SelectedCustomerId.HasValue && Customers.All(customer => customer.Id != EntryForm.SelectedCustomerId.Value))
        {
            EntryForm.SelectedCustomerId = null;
        }
    }

    private async Task LoadProductServicesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ProductServiceListItemDto> items = await _productCatalogService.ListAsync(cancellationToken);

        ProductServices.Clear();

        foreach (ProductServiceListItemDto item in items)
        {
            ProductServices.Add(new ProductServiceListItemViewModel(
                id: item.Id,
                name: item.Name,
                category: item.Category,
                unitPrice: item.UnitPrice,
                isService: item.IsService,
                isActive: item.IsActive));
        }

        if (EntryForm.SelectedProductServiceId.HasValue && ProductServices.All(item => item.Id != EntryForm.SelectedProductServiceId.Value))
        {
            EntryForm.SelectedProductServiceId = null;
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

    private void SetCustomerDeleteConfirmation(Guid customerId)
    {
        _isCustomerDeleteConfirmationPending = true;
        _deleteConfirmationCustomerId = customerId;
        OnPropertyChanged(nameof(IsCustomerDeleteConfirmationPending));
        OnPropertyChanged(nameof(DeleteSelectedCustomerButtonLabel));
    }

    private void ResetCustomerDeleteConfirmation()
    {
        if (!_isCustomerDeleteConfirmationPending && _deleteConfirmationCustomerId is null)
        {
            return;
        }

        _isCustomerDeleteConfirmationPending = false;
        _deleteConfirmationCustomerId = null;
        OnPropertyChanged(nameof(IsCustomerDeleteConfirmationPending));
        OnPropertyChanged(nameof(DeleteSelectedCustomerButtonLabel));
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

    private void OnProductServicesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ProductServicesSummary));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
