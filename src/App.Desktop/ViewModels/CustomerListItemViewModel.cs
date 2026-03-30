namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class CustomerListItemViewModel
{
    public CustomerListItemViewModel(Guid id, string name, string? email, string? phone)
    {
        Id = id;
        Name = name;
        Email = email;
        Phone = phone;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string? Email { get; }

    public string? Phone { get; }

    public string EmailDisplay => string.IsNullOrWhiteSpace(Email) ? "-" : Email;

    public string PhoneDisplay => string.IsNullOrWhiteSpace(Phone) ? "-" : Phone;
}
