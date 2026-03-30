using System.Globalization;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

public sealed class ProductServiceListItemViewModel
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");

    public ProductServiceListItemViewModel(
        Guid id,
        string name,
        string category,
        decimal unitPrice,
        bool isService,
        bool isActive)
    {
        Id = id;
        Name = name;
        Category = category;
        UnitPrice = unitPrice;
        IsService = isService;
        IsActive = isActive;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Category { get; }

    public decimal UnitPrice { get; }

    public bool IsService { get; }

    public bool IsActive { get; }

    public string KindDisplay => IsService ? "Serviço" : "Produto";

    public string UnitPriceDisplay => UnitPrice.ToString("C", PortugueseCulture);

    public string ActiveDisplay => IsActive ? "Ativo" : "Inativo";
}
