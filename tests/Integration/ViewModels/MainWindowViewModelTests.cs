using SMEFinanceSuite.App.Desktop.ViewModels;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.Customers;
using SMEFinanceSuite.Core.Application.Dashboard;
using SMEFinanceSuite.Core.Application.FinancialEntries;
using Xunit;

namespace SMEFinanceSuite.Tests.Integration.ViewModels;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task DeleteSelectedCustomerAsync_ShouldRequireTwoStepConfirmation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var customerId = Guid.NewGuid();
        var customerService = new FakeCustomerService(
            [
                new CustomerListItemDto(
                    Id: customerId,
                    Name: "Cliente ViewModel",
                    Email: null,
                    Phone: null)
            ]);

        var viewModel = new MainWindowViewModel(
            financialDashboardService: new FakeDashboardService(),
            financialEntryService: new FakeFinancialEntryService(),
            customerService: customerService);

        await viewModel.InitializeAsync(cancellationToken);
        viewModel.SelectedCustomer = Assert.Single(viewModel.Customers);

        await viewModel.DeleteSelectedCustomerAsync(cancellationToken);

        Assert.True(viewModel.IsCustomerDeleteConfirmationPending);
        Assert.Equal("Confirmar exclusão", viewModel.DeleteSelectedCustomerButtonLabel);
        Assert.Equal(0, customerService.DeleteCalls);

        await viewModel.DeleteSelectedCustomerAsync(cancellationToken);

        Assert.False(viewModel.IsCustomerDeleteConfirmationPending);
        Assert.Equal("Excluir selecionado", viewModel.DeleteSelectedCustomerButtonLabel);
        Assert.Equal(1, customerService.DeleteCalls);
        Assert.Empty(viewModel.Customers);
    }

    private sealed class FakeDashboardService : IFinancialDashboardService
    {
        public Task<DashboardSummaryDto> GetSummaryAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DashboardSummaryDto.Empty);
        }
    }

    private sealed class FakeFinancialEntryService : IFinancialEntryService
    {
        public Task<Guid> RegisterAsync(CreateFinancialEntryCommand command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(UpdateFinancialEntryCommand command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<FinancialEntryListItemDto>> ListAsync(FinancialEntriesFilter? filter = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<FinancialEntryListItemDto>>([]);
        }
    }

    private sealed class FakeCustomerService(List<CustomerListItemDto> seedCustomers) : ICustomerService
    {
        private readonly List<CustomerListItemDto> _customers = seedCustomers;

        public int DeleteCalls { get; private set; }

        public Task<Guid> RegisterAsync(CreateCustomerCommand command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(UpdateCustomerCommand command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            _customers.RemoveAll(customer => customer.Id == id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CustomerListItemDto>> ListAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CustomerListItemDto> snapshot = _customers.ToList();
            return Task.FromResult(snapshot);
        }
    }
}
