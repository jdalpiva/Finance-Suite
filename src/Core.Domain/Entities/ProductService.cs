using SMEFinanceSuite.Core.Domain.Common;

namespace SMEFinanceSuite.Core.Domain.Entities;

public sealed class ProductService : AuditableEntity
{
    private ProductService()
    {
    }

    public ProductService(string name, string category, decimal unitPrice, bool isService)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name), 120);
        Category = Guard.AgainstNullOrWhiteSpace(category, nameof(category), 80);
        UnitPrice = Guard.AgainstInvalidMonetaryAmount(unitPrice, nameof(unitPrice));
        IsService = isService;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public decimal UnitPrice { get; private set; }

    public bool IsService { get; private set; }

    public bool IsActive { get; private set; }

    public void UpdatePricing(decimal unitPrice)
    {
        UnitPrice = Guard.AgainstInvalidMonetaryAmount(unitPrice, nameof(unitPrice));
        Touch();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        Touch();
    }
}
