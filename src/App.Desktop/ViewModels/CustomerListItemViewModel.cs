namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class CustomerListItemViewModel
{
    public CustomerListItemViewModel(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; }

    public string Name { get; }
}
