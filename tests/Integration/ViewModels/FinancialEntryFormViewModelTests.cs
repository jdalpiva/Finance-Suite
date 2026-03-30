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
}
