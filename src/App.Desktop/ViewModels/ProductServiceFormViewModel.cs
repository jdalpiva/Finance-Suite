using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SMEFinanceSuite.Core.Application.ProductServices;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class ProductServiceFormViewModel : INotifyPropertyChanged
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");

    private string _name = string.Empty;
    private string _category = string.Empty;
    private string _unitPrice = string.Empty;
    private string _selectedKind = "Serviço";
    private bool _isActive = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<string> KindOptions { get; } = ["Serviço", "Produto"];

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    public string UnitPrice
    {
        get => _unitPrice;
        set => SetProperty(ref _unitPrice, value);
    }

    public string SelectedKind
    {
        get => _selectedKind;
        set => SetProperty(ref _selectedKind, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public CreateProductServiceCommand BuildCreateCommand()
    {
        (string name, string category, decimal unitPrice, bool isService) = BuildInput();

        return new CreateProductServiceCommand(
            Name: name,
            Category: category,
            UnitPrice: unitPrice,
            IsService: isService,
            IsActive: IsActive);
    }

    public UpdateProductServiceCommand BuildUpdateCommand(Guid id)
    {
        (string name, string category, decimal unitPrice, bool isService) = BuildInput();

        return new UpdateProductServiceCommand(
            Id: id,
            Name: name,
            Category: category,
            UnitPrice: unitPrice,
            IsService: isService,
            IsActive: IsActive);
    }

    public void FillFromSelection(ProductServiceListItemViewModel selectedItem)
    {
        ArgumentNullException.ThrowIfNull(selectedItem);

        Name = selectedItem.Name;
        Category = selectedItem.Category;
        UnitPrice = selectedItem.UnitPrice.ToString("0.00", PortugueseCulture);
        SelectedKind = selectedItem.KindDisplay;
        IsActive = selectedItem.IsActive;
    }

    public void Clear()
    {
        Name = string.Empty;
        Category = string.Empty;
        UnitPrice = string.Empty;
        SelectedKind = KindOptions[0];
        IsActive = true;
    }

    private (string Name, string Category, decimal UnitPrice, bool IsService) BuildInput()
    {
        string name = Name.Trim();
        string category = Category.Trim();

        if (!TryParseAmount(UnitPrice, out decimal unitPrice))
        {
            throw new InvalidOperationException("Informe um preço válido. Exemplo: 2500,00");
        }

        bool isService = SelectedKind == "Serviço";

        return (name, category, unitPrice, isService);
    }

    private static bool TryParseAmount(string rawAmount, out decimal amount)
    {
        if (decimal.TryParse(rawAmount, NumberStyles.Number, PortugueseCulture, out amount))
        {
            return true;
        }

        return decimal.TryParse(rawAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
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
