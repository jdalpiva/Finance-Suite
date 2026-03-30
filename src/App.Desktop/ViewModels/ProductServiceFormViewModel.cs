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

    public CreateProductServiceCommand BuildCreateCommand()
    {
        string name = Name.Trim();
        string category = Category.Trim();

        if (!TryParseAmount(UnitPrice, out decimal unitPrice))
        {
            throw new InvalidOperationException("Informe um preço válido. Exemplo: 2500,00");
        }

        bool isService = SelectedKind == "Serviço";

        return new CreateProductServiceCommand(
            Name: name,
            Category: category,
            UnitPrice: unitPrice,
            IsService: isService);
    }

    public void Clear()
    {
        Name = string.Empty;
        Category = string.Empty;
        UnitPrice = string.Empty;
        SelectedKind = KindOptions[0];
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
