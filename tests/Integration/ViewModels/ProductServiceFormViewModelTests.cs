using SMEFinanceSuite.App.Desktop.ViewModels;
using Xunit;

namespace SMEFinanceSuite.Tests.Integration.ViewModels;

public sealed class ProductServiceFormViewModelTests
{
    [Fact]
    public void BuildCreateCommand_ShouldParseUnitPriceWhenInputIsValid()
    {
        var viewModel = new ProductServiceFormViewModel
        {
            Name = "Plano Plus",
            Category = "Serviços",
            UnitPrice = "2500,40",
            SelectedKind = "Serviço",
            IsActive = true
        };

        var command = viewModel.BuildCreateCommand();

        Assert.Equal(2500.40m, command.UnitPrice);
    }

    [Fact]
    public void BuildCreateCommand_ShouldThrowWhenUnitPriceIsInvalid()
    {
        var viewModel = new ProductServiceFormViewModel
        {
            Name = "Plano Plus",
            Category = "Serviços",
            UnitPrice = "invalido",
            SelectedKind = "Serviço",
            IsActive = true
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => viewModel.BuildCreateCommand());

        Assert.Equal("Informe um preço válido. Exemplo: 2500,00", exception.Message);
    }
}
