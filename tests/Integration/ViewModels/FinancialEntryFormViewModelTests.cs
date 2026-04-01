using SMEFinanceSuite.App.Desktop.ViewModels;
using Xunit;

namespace SMEFinanceSuite.Tests.Integration.ViewModels;

public sealed class FinancialEntryFormViewModelTests
{
    [Fact]
    public void BuildUpdateCommand_ShouldPropagateSelectedCustomerId()
    {
        var selectedCustomerId = Guid.NewGuid();
        var selectedProductServiceId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        var viewModel = new FinancialEntryFormViewModel
        {
            NewEntryDescription = "Receita recorrente",
            NewEntryAmount = "1200,50",
            NewEntryOccurredOn = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"),
            SelectedFormEntryType = "Receita",
            NewEntryNotes = "Atualização com vínculo",
            SelectedCustomerId = selectedCustomerId,
            SelectedProductServiceId = selectedProductServiceId
        };

        var command = viewModel.BuildUpdateCommand(entryId);

        Assert.Equal(entryId, command.Id);
        Assert.Equal(selectedCustomerId, command.CustomerId);
        Assert.Equal(selectedProductServiceId, command.ProductServiceId);
    }

    [Fact]
    public void BuildCreateCommand_ShouldParseAmountWhenInputIsValid()
    {
        var viewModel = new FinancialEntryFormViewModel
        {
            NewEntryDescription = "Receita válida",
            NewEntryAmount = "1500,50",
            NewEntryOccurredOn = "2026-04-01",
            SelectedFormEntryType = "Receita"
        };

        var command = viewModel.BuildCreateCommand();

        Assert.Equal(1500.50m, command.Amount);
    }

    [Fact]
    public void BuildCreateCommand_ShouldParseAmountWithDotDecimalSeparator()
    {
        var viewModel = new FinancialEntryFormViewModel
        {
            NewEntryDescription = "Receita fallback invariant",
            NewEntryAmount = "1500.50",
            NewEntryOccurredOn = "2026-04-01",
            SelectedFormEntryType = "Receita"
        };

        var command = viewModel.BuildCreateCommand();

        Assert.Equal(1500.50m, command.Amount);
    }

    [Fact]
    public void BuildCreateCommand_ShouldParseAmountWithGroupedThousandsInPtBrFormat()
    {
        var viewModel = new FinancialEntryFormViewModel
        {
            NewEntryDescription = "Receita com milhar",
            NewEntryAmount = "1.500,50",
            NewEntryOccurredOn = "2026-04-01",
            SelectedFormEntryType = "Receita"
        };

        var command = viewModel.BuildCreateCommand();

        Assert.Equal(1500.50m, command.Amount);
    }

    [Fact]
    public void BuildCreateCommand_ShouldThrowWhenAmountIsInvalid()
    {
        var viewModel = new FinancialEntryFormViewModel
        {
            NewEntryDescription = "Receita inválida",
            NewEntryAmount = "abc",
            NewEntryOccurredOn = "2026-04-01",
            SelectedFormEntryType = "Receita"
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => viewModel.BuildCreateCommand());

        Assert.Equal("Informe um valor válido. Exemplo: 1500,50", exception.Message);
    }
}
