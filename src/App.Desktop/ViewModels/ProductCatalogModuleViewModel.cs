using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.ProductServices;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class ProductCatalogModuleViewModel : INotifyPropertyChanged
{
    private readonly IProductCatalogService _productCatalogService;
    private ProductServiceListItemViewModel? _selectedProductService;
    private bool _isBusy;
    private bool _isDeleteConfirmationPending;
    private Guid? _deleteConfirmationProductServiceId;

    public ProductCatalogModuleViewModel(IProductCatalogService productCatalogService)
    {
        _productCatalogService = productCatalogService;
        ProductServices.CollectionChanged += OnProductServicesChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ProductServiceFormViewModel Form { get; } = new();

    public ObservableCollection<ProductServiceListItemViewModel> ProductServices { get; } = [];

    public ObservableCollection<ProductServiceListItemViewModel> ActiveProductServices { get; } = [];

    public string Summary => $"{ProductServices.Count} itens cadastrados";

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanRegister));
            OnPropertyChanged(nameof(CanUpdateSelected));
            OnPropertyChanged(nameof(CanDeleteSelected));
        }
    }

    public bool CanRegister => !IsBusy;

    public bool CanUpdateSelected => !IsBusy && SelectedProductService is not null;

    public bool CanDeleteSelected => !IsBusy && SelectedProductService is not null;

    public bool IsDeleteConfirmationPending => _isDeleteConfirmationPending;

    public string DeleteSelectedButtonLabel => IsDeleteConfirmationPending ? "Confirmar exclusão" : "Excluir selecionado";

    public ProductServiceListItemViewModel? SelectedProductService
    {
        get => _selectedProductService;
        set
        {
            if (!SetProperty(ref _selectedProductService, value))
            {
                return;
            }

            ResetDeleteConfirmation();
            OnPropertyChanged(nameof(CanUpdateSelected));
            OnPropertyChanged(nameof(CanDeleteSelected));

            if (value is null)
            {
                Form.Clear();
                return;
            }

            Form.FillFromSelection(value);
        }
    }

    public void SetBusy(bool isBusy)
    {
        IsBusy = isBusy;
    }

    public bool ContainsProductService(Guid id)
    {
        return ProductServices.Any(item => item.Id == id);
    }

    public bool IsActiveProductService(Guid id)
    {
        return ProductServices.Any(item => item.Id == id && item.IsActive);
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default, Guid? selectedProductServiceId = null)
    {
        IReadOnlyList<ProductServiceListItemDto> items = await _productCatalogService.ListAsync(cancellationToken);

        Guid? selectedProductServiceIdToRestore = selectedProductServiceId ?? SelectedProductService?.Id;

        ProductServices.Clear();
        ActiveProductServices.Clear();

        foreach (ProductServiceListItemDto item in items)
        {
            ProductServiceListItemViewModel viewModel = new(
                id: item.Id,
                name: item.Name,
                category: item.Category,
                unitPrice: item.UnitPrice,
                isService: item.IsService,
                isActive: item.IsActive);

            ProductServices.Add(viewModel);

            if (item.IsActive)
            {
                ActiveProductServices.Add(viewModel);
            }
        }

        if (selectedProductServiceIdToRestore is null)
        {
            SelectedProductService = null;
        }
        else
        {
            SelectedProductService = ProductServices.FirstOrDefault(item => item.Id == selectedProductServiceIdToRestore.Value);
        }
    }

    public async Task RegisterAsync(CancellationToken cancellationToken = default)
    {
        CreateProductServiceCommand command = Form.BuildCreateCommand();
        await _productCatalogService.RegisterAsync(command, cancellationToken);

        Form.Clear();
        await LoadAsync(cancellationToken);
    }

    public async Task UpdateSelectedAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedProductService is null)
        {
            throw new InvalidOperationException("Selecione um produto/serviço para atualizar.");
        }

        UpdateProductServiceCommand command = Form.BuildUpdateCommand(SelectedProductService.Id);
        await _productCatalogService.UpdateAsync(command, cancellationToken);

        ResetDeleteConfirmation();
        await LoadAsync(cancellationToken, selectedProductServiceId: command.Id);
    }

    public ProductDeleteRequest RequestDeleteSelected()
    {
        if (SelectedProductService is null)
        {
            return new ProductDeleteRequest(ProductDeleteRequestState.NoSelection);
        }

        if (!IsDeleteConfirmationPending || _deleteConfirmationProductServiceId != SelectedProductService.Id)
        {
            SetDeleteConfirmation(SelectedProductService.Id);
            return new ProductDeleteRequest(ProductDeleteRequestState.ConfirmationRequired);
        }

        return new ProductDeleteRequest(ProductDeleteRequestState.Ready, SelectedProductService.Id);
    }

    public async Task DeleteConfirmedAsync(Guid productServiceId, CancellationToken cancellationToken = default)
    {
        await _productCatalogService.DeleteAsync(productServiceId, cancellationToken);

        SelectedProductService = null;
        ResetDeleteConfirmation();
        await LoadAsync(cancellationToken);
    }

    private void SetDeleteConfirmation(Guid productServiceId)
    {
        _isDeleteConfirmationPending = true;
        _deleteConfirmationProductServiceId = productServiceId;
        OnPropertyChanged(nameof(IsDeleteConfirmationPending));
        OnPropertyChanged(nameof(DeleteSelectedButtonLabel));
    }

    private void ResetDeleteConfirmation()
    {
        if (!_isDeleteConfirmationPending && _deleteConfirmationProductServiceId is null)
        {
            return;
        }

        _isDeleteConfirmationPending = false;
        _deleteConfirmationProductServiceId = null;
        OnPropertyChanged(nameof(IsDeleteConfirmationPending));
        OnPropertyChanged(nameof(DeleteSelectedButtonLabel));
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnProductServicesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Summary));
    }
}

public enum ProductDeleteRequestState
{
    NoSelection,
    ConfirmationRequired,
    Ready
}

public readonly record struct ProductDeleteRequest(ProductDeleteRequestState State, Guid TargetProductServiceId = default);
