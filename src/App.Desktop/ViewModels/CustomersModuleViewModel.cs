using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.Customers;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class CustomersModuleViewModel : INotifyPropertyChanged
{
    private readonly ICustomerService _customerService;
    private CustomerListItemViewModel? _selectedCustomer;
    private bool _isBusy;
    private bool _isDeleteConfirmationPending;
    private Guid? _deleteConfirmationCustomerId;

    public event PropertyChangedEventHandler? PropertyChanged;

    public CustomerFormViewModel Form { get; } = new();

    public ObservableCollection<CustomerListItemViewModel> Customers { get; } = [];

    public CustomersModuleViewModel(ICustomerService customerService)
    {
        _customerService = customerService;
        Customers.CollectionChanged += OnCustomersChanged;
    }

    public string Summary => $"{Customers.Count} clientes cadastrados";

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

    public bool CanUpdateSelected => !IsBusy && SelectedCustomer is not null;

    public bool CanDeleteSelected => !IsBusy && SelectedCustomer is not null;

    public bool IsDeleteConfirmationPending => _isDeleteConfirmationPending;

    public string DeleteSelectedButtonLabel => IsDeleteConfirmationPending ? "Confirmar exclusão" : "Excluir selecionado";

    public CustomerListItemViewModel? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (!SetProperty(ref _selectedCustomer, value))
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

    public bool ContainsCustomer(Guid id)
    {
        return Customers.Any(customer => customer.Id == id);
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default, Guid? selectedCustomerId = null)
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
    }

    public async Task RegisterAsync(CancellationToken cancellationToken = default)
    {
        CreateCustomerCommand command = Form.BuildCreateCommand();
        await _customerService.RegisterAsync(command, cancellationToken);

        SelectedCustomer = null;
        ResetDeleteConfirmation();
        await LoadAsync(cancellationToken);
    }

    public async Task UpdateSelectedAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedCustomer is null)
        {
            throw new InvalidOperationException("Selecione um cliente para atualizar.");
        }

        UpdateCustomerCommand command = Form.BuildUpdateCommand(SelectedCustomer.Id);
        await _customerService.UpdateAsync(command, cancellationToken);

        ResetDeleteConfirmation();
        await LoadAsync(cancellationToken, selectedCustomerId: command.Id);
    }

    public CustomerDeleteRequest RequestDeleteSelected()
    {
        if (SelectedCustomer is null)
        {
            return new CustomerDeleteRequest(CustomerDeleteRequestState.NoSelection);
        }

        if (!IsDeleteConfirmationPending || _deleteConfirmationCustomerId != SelectedCustomer.Id)
        {
            SetDeleteConfirmation(SelectedCustomer.Id);
            return new CustomerDeleteRequest(CustomerDeleteRequestState.ConfirmationRequired);
        }

        return new CustomerDeleteRequest(CustomerDeleteRequestState.Ready, SelectedCustomer.Id);
    }

    public async Task DeleteConfirmedAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        await _customerService.DeleteAsync(customerId, cancellationToken);

        SelectedCustomer = null;
        ResetDeleteConfirmation();
        await LoadAsync(cancellationToken);
    }

    private void SetDeleteConfirmation(Guid customerId)
    {
        _isDeleteConfirmationPending = true;
        _deleteConfirmationCustomerId = customerId;
        OnPropertyChanged(nameof(IsDeleteConfirmationPending));
        OnPropertyChanged(nameof(DeleteSelectedButtonLabel));
    }

    private void ResetDeleteConfirmation()
    {
        if (!_isDeleteConfirmationPending && _deleteConfirmationCustomerId is null)
        {
            return;
        }

        _isDeleteConfirmationPending = false;
        _deleteConfirmationCustomerId = null;
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

    private void OnCustomersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Summary));
    }
}

public enum CustomerDeleteRequestState
{
    NoSelection,
    ConfirmationRequired,
    Ready
}

public readonly record struct CustomerDeleteRequest(CustomerDeleteRequestState State, Guid TargetCustomerId = default);
