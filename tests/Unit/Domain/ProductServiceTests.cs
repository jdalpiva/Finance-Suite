using SMEFinanceSuite.Core.Domain.Entities;
using Xunit;

namespace SMEFinanceSuite.Tests.Unit.Domain;

public sealed class ProductServiceTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenUnitPriceHasMoreThanTwoDecimalPlaces()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProductService("Plano Pro", "Serviços", 99.999m, isService: true));
    }

    [Fact]
    public void UpdatePricing_ShouldThrow_WhenUnitPriceHasMoreThanTwoDecimalPlaces()
    {
        var productService = new ProductService("Plano Base", "Serviços", 100m, isService: true);

        Assert.Throws<ArgumentOutOfRangeException>(() => productService.UpdatePricing(120.555m));
    }
}
