using System.ComponentModel;
using System.Runtime.CompilerServices;
using SMEFinanceSuite.Core.Application.Customers;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class CustomerFormViewModel : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _email = string.Empty;
    private string _phone = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public CreateCustomerCommand BuildCreateCommand()
    {
        return new CreateCustomerCommand(
            Name: NormalizeRequired(Name),
            Email: NormalizeOptional(Email),
            Phone: NormalizeOptional(Phone));
    }

    public UpdateCustomerCommand BuildUpdateCommand(Guid id)
    {
        return new UpdateCustomerCommand(
            Id: id,
            Name: NormalizeRequired(Name),
            Email: NormalizeOptional(Email),
            Phone: NormalizeOptional(Phone));
    }

    public void FillFromSelection(CustomerListItemViewModel selectedCustomer)
    {
        ArgumentNullException.ThrowIfNull(selectedCustomer);

        Name = selectedCustomer.Name;
        Email = selectedCustomer.Email ?? string.Empty;
        Phone = selectedCustomer.Phone ?? string.Empty;
    }

    public void Clear()
    {
        Name = string.Empty;
        Email = string.Empty;
        Phone = string.Empty;
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim();
    }

    private static string? NormalizeOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
