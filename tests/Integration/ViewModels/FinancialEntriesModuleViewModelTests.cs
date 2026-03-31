using SMEFinanceSuite.App.Desktop.ViewModels;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.Customers;
using SMEFinanceSuite.Core.Application.FinancialEntries;
using SMEFinanceSuite.Core.Application.ProductServices;
using SMEFinanceSuite.Core.Domain.Enums;
using Xunit;

namespace SMEFinanceSuite.Tests.Integration.ViewModels;

public sealed class FinancialEntriesModuleViewModelTests
{
    [Fact]
    public async Task DeleteConfirmedAsync_ShouldRequireTwoStepConfirmation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var entryId = Guid.NewGuid();
        var service = new FakeFinancialEntryService(
            [
                new FinancialEntryListItemDto(
                    Id: entryId,
                    Description: "Mensalidade",
                    Amount: 250m,
                    OccurredOn: new DateOnly(2026, 3, 10),
                    EntryType: EntryType.Revenue,
                    CustomerId: null,
                    ProductServiceId: null,
                    Notes: null)
            ]);

        var viewModel = new FinancialEntriesModuleViewModel(service);

        await viewModel.LoadAsync(cancellationToken);
        viewModel.SelectedFinancialEntry = Assert.Single(viewModel.FinancialEntries);

        FinancialEntryDeleteRequest firstRequest = viewModel.RequestDeleteSelected();

        Assert.Equal(FinancialEntryDeleteRequestState.ConfirmationRequired, firstRequest.State);
        Assert.True(viewModel.IsDeleteConfirmationPending);
        Assert.Equal("Confirmar exclusão", viewModel.DeleteSelectedButtonLabel);
        Assert.Equal(0, service.DeleteCalls);

        FinancialEntryDeleteRequest secondRequest = viewModel.RequestDeleteSelected();

        Assert.Equal(FinancialEntryDeleteRequestState.Ready, secondRequest.State);
        Assert.Equal(entryId, secondRequest.TargetEntryId);

        await viewModel.DeleteConfirmedAsync(secondRequest.TargetEntryId, cancellationToken);

        Assert.False(viewModel.IsDeleteConfirmationPending);
        Assert.Equal("Excluir selecionado", viewModel.DeleteSelectedButtonLabel);
        Assert.Equal(1, service.DeleteCalls);
        Assert.Empty(viewModel.FinancialEntries);
    }

    [Fact]
    public async Task SyncFormReferences_ShouldClearMissingOrInactiveReferences()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var customerId = Guid.NewGuid();
        var inactiveProductId = Guid.NewGuid();

        var entriesModule = new FinancialEntriesModuleViewModel(new FakeFinancialEntryService([]));
        entriesModule.Form.SelectedCustomerId = customerId;
        entriesModule.Form.SelectedProductServiceId = inactiveProductId;

        var customersModule = new CustomersModuleViewModel(new FakeCustomerService([]));
        var productCatalogModule = new ProductCatalogModuleViewModel(
            new FakeProductCatalogService(
                [
                    new ProductServiceListItemDto(
                        Id: inactiveProductId,
                        Name: "Item inativo",
                        Category: "Serviços",
                        UnitPrice: 100m,
                        IsService: true,
                        IsActive: false)
                ]));

        await customersModule.LoadAsync(cancellationToken);
        await productCatalogModule.LoadAsync(cancellationToken);

        entriesModule.SyncFormReferences(customersModule, productCatalogModule);

        Assert.Null(entriesModule.Form.SelectedCustomerId);
        Assert.Null(entriesModule.Form.SelectedProductServiceId);
    }

    private sealed class FakeFinancialEntryService(List<FinancialEntryListItemDto> seedEntries) : IFinancialEntryService
    {
        private readonly List<FinancialEntryListItemDto> _entries = seedEntries;

        public int DeleteCalls { get; private set; }

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
            DeleteCalls++;
            _entries.RemoveAll(entry => entry.Id == id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FinancialEntryListItemDto>> ListAsync(FinancialEntriesFilter? filter = null, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<FinancialEntryListItemDto> snapshot = _entries.ToList();
            return Task.FromResult(snapshot);
        }
    }

    private sealed class FakeCustomerService(List<CustomerListItemDto> seedCustomers) : ICustomerService
    {
        private readonly List<CustomerListItemDto> _customers = seedCustomers;

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
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<CustomerListItemDto>> ListAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CustomerListItemDto> snapshot = _customers.ToList();
            return Task.FromResult(snapshot);
        }
    }

    private sealed class FakeProductCatalogService(List<ProductServiceListItemDto> seedItems) : IProductCatalogService
    {
        private readonly List<ProductServiceListItemDto> _items = seedItems;

        public Task<Guid> RegisterAsync(CreateProductServiceCommand command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(UpdateProductServiceCommand command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<ProductServiceListItemDto>> ListAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ProductServiceListItemDto> snapshot = _items.ToList();
            return Task.FromResult(snapshot);
        }
    }
}
