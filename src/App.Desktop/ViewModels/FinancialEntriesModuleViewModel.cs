using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.FinancialEntries;
using SMEFinanceSuite.Core.Domain.Enums;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class FinancialEntriesModuleViewModel : INotifyPropertyChanged
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IFinancialEntryService _financialEntryService;

    private string _filterFrom = string.Empty;
    private string _filterTo = string.Empty;
    private string _selectedFilterType = "Todos";

    private FinancialEntryListItemViewModel? _selectedFinancialEntry;
    private bool _isBusy;
    private bool _isDeleteConfirmationPending;
    private Guid? _deleteConfirmationEntryId;

    public FinancialEntriesModuleViewModel(IFinancialEntryService financialEntryService)
    {
        _financialEntryService = financialEntryService;
        FinancialEntries.CollectionChanged += OnFinancialEntriesChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public FinancialEntryFormViewModel Form { get; } = new();

    public ObservableCollection<FinancialEntryListItemViewModel> FinancialEntries { get; } = [];

    public IReadOnlyList<string> FilterTypeOptions { get; } = ["Todos", "Receita", "Despesa"];

    public string EntriesSummary => FinancialEntries.Count == 0
        ? "Nenhum lançamento encontrado"
        : $"{FinancialEntries.Count} lançamentos";

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
            OnPropertyChanged(nameof(CanRegister));
            OnPropertyChanged(nameof(CanUpdateSelected));
            OnPropertyChanged(nameof(CanDeleteSelected));
        }
    }

    public bool IsDeleteConfirmationPending => _isDeleteConfirmationPending;

    public string DeleteSelectedButtonLabel => IsDeleteConfirmationPending ? "Confirmar exclusão" : "Excluir selecionado";

    public bool CanApplyFilters => !IsBusy;

    public bool CanClearFilters => !IsBusy;

    public bool CanRegister => !IsBusy;

    public bool CanUpdateSelected => !IsBusy && SelectedFinancialEntry is not null;

    public bool CanDeleteSelected => !IsBusy && SelectedFinancialEntry is not null;

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
            OnPropertyChanged(nameof(CanUpdateSelected));
            OnPropertyChanged(nameof(CanDeleteSelected));

            if (value is not null)
            {
                Form.FillFromSelection(value);
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

    public void SetBusy(bool isBusy)
    {
        IsBusy = isBusy;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default, Guid? selectedEntryId = null)
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

    public async Task ApplyFiltersAsync(CancellationToken cancellationToken = default)
    {
        ResetDeleteConfirmation();
        await LoadAsync(cancellationToken);
    }

    public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
    {
        FilterFrom = string.Empty;
        FilterTo = string.Empty;
        SelectedFilterType = FilterTypeOptions[0];

        ResetDeleteConfirmation();
        await LoadAsync(cancellationToken);
    }

    public async Task RegisterAsync(CancellationToken cancellationToken = default)
    {
        CreateFinancialEntryCommand command = Form.BuildCreateCommand();
        await _financialEntryService.RegisterAsync(command, cancellationToken);

        SelectedFinancialEntry = null;
        Form.Clear();
        ResetDeleteConfirmation();
        await LoadAsync(cancellationToken);
    }

    public async Task UpdateSelectedAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedFinancialEntry is null)
        {
            throw new InvalidOperationException("Selecione um lançamento para atualizar.");
        }

        UpdateFinancialEntryCommand command = Form.BuildUpdateCommand(SelectedFinancialEntry.Id);
        await _financialEntryService.UpdateAsync(command, cancellationToken);

        ResetDeleteConfirmation();
        await LoadAsync(cancellationToken, selectedEntryId: command.Id);
    }

    public FinancialEntryDeleteRequest RequestDeleteSelected()
    {
        if (SelectedFinancialEntry is null)
        {
            return new FinancialEntryDeleteRequest(FinancialEntryDeleteRequestState.NoSelection);
        }

        if (!IsDeleteConfirmationPending || _deleteConfirmationEntryId != SelectedFinancialEntry.Id)
        {
            SetDeleteConfirmation(SelectedFinancialEntry.Id);
            return new FinancialEntryDeleteRequest(FinancialEntryDeleteRequestState.ConfirmationRequired);
        }

        return new FinancialEntryDeleteRequest(FinancialEntryDeleteRequestState.Ready, SelectedFinancialEntry.Id);
    }

    public async Task DeleteConfirmedAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        await _financialEntryService.DeleteAsync(entryId, cancellationToken);

        SelectedFinancialEntry = null;
        Form.Clear();
        ResetDeleteConfirmation();
        await LoadAsync(cancellationToken);
    }

    public void SyncFormReferences(CustomersModuleViewModel customersModule, ProductCatalogModuleViewModel productCatalogModule)
    {
        ArgumentNullException.ThrowIfNull(customersModule);
        ArgumentNullException.ThrowIfNull(productCatalogModule);

        if (Form.SelectedCustomerId.HasValue && !customersModule.ContainsCustomer(Form.SelectedCustomerId.Value))
        {
            Form.SelectedCustomerId = null;
        }

        if (!Form.SelectedProductServiceId.HasValue)
        {
            return;
        }

        Guid selectedProductServiceId = Form.SelectedProductServiceId.Value;

        if (!productCatalogModule.ContainsProductService(selectedProductServiceId)
            || !productCatalogModule.IsActiveProductService(selectedProductServiceId))
        {
            Form.SelectedProductServiceId = null;
        }
    }

    private FinancialEntriesFilter BuildFilter()
    {
        DateOnly? from = DateOnlyInputParser.ParseOptional(FilterFrom, "Data inicial");
        DateOnly? to = DateOnlyInputParser.ParseOptional(FilterTo, "Data final");

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
        OnPropertyChanged(nameof(DeleteSelectedButtonLabel));
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
        OnPropertyChanged(nameof(DeleteSelectedButtonLabel));
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

public enum FinancialEntryDeleteRequestState
{
    NoSelection,
    ConfirmationRequired,
    Ready
}

public readonly record struct FinancialEntryDeleteRequest(FinancialEntryDeleteRequestState State, Guid TargetEntryId = default);
