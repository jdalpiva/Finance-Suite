using SMEFinanceSuite.App.Desktop.ViewModels;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.Customers;
using SMEFinanceSuite.Core.Application.Dashboard;
using SMEFinanceSuite.Core.Application.FinancialEntries;
using SMEFinanceSuite.Core.Application.ProductServices;
using SMEFinanceSuite.Core.Application.Reports;
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
            financialReportsService: new FakeReportsService(),
            financialReportCsvExporter: new FakeReportCsvExporter(),
            financialEntryService: new FakeFinancialEntryService(),
            customerService: customerService,
            productCatalogService: new FakeProductCatalogService());

        await viewModel.InitializeAsync(cancellationToken);
        viewModel.CustomersModule.SelectedCustomer = Assert.Single(viewModel.CustomersModule.Customers);

        await viewModel.DeleteSelectedCustomerAsync(cancellationToken);

        Assert.True(viewModel.CustomersModule.IsDeleteConfirmationPending);
        Assert.Equal("Confirmar exclusão", viewModel.CustomersModule.DeleteSelectedButtonLabel);
        Assert.Equal(0, customerService.DeleteCalls);

        await viewModel.DeleteSelectedCustomerAsync(cancellationToken);

        Assert.False(viewModel.CustomersModule.IsDeleteConfirmationPending);
        Assert.Equal("Excluir selecionado", viewModel.CustomersModule.DeleteSelectedButtonLabel);
        Assert.Equal(1, customerService.DeleteCalls);
        Assert.Empty(viewModel.CustomersModule.Customers);
    }

    [Fact]
    public async Task ToggleSelectedProductServiceActiveAsync_ShouldUpdateAvailabilityAndSyncFormReferences()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var productServiceId = Guid.NewGuid();
        var productCatalogService = new FakeProductCatalogService(
            [
                new ProductServiceListItemDto(
                    Id: productServiceId,
                    Name: "Plano Ativo",
                    Category: "Serviços",
                    UnitPrice: 120m,
                    IsService: true,
                    IsActive: true)
            ]);

        var viewModel = new MainWindowViewModel(
            financialDashboardService: new FakeDashboardService(),
            financialReportsService: new FakeReportsService(),
            financialReportCsvExporter: new FakeReportCsvExporter(),
            financialEntryService: new FakeFinancialEntryService(),
            customerService: new FakeCustomerService([]),
            productCatalogService: productCatalogService);

        await viewModel.InitializeAsync(cancellationToken);

        viewModel.ProductCatalogModule.SelectedProductService = Assert.Single(viewModel.ProductCatalogModule.ProductServices);
        viewModel.FinancialEntriesModule.Form.SelectedProductServiceId = productServiceId;

        await viewModel.ToggleSelectedProductServiceActiveAsync(cancellationToken);

        ProductServiceListItemViewModel currentItem = Assert.Single(viewModel.ProductCatalogModule.ProductServices);
        Assert.False(currentItem.IsActive);
        Assert.Empty(viewModel.ProductCatalogModule.ActiveProductServices);
        Assert.Null(viewModel.FinancialEntriesModule.Form.SelectedProductServiceId);
        Assert.Equal(1, productCatalogService.UpdateCalls);
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

    private sealed class FakeReportsService : IFinancialReportsService
    {
        public Task<FinancialReportSummaryDto> GetSummaryAsync(FinancialReportFilter? filter = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(FinancialReportSummaryDto.Empty);
        }
    }

    private sealed class FakeReportCsvExporter : IFinancialReportCsvExporter
    {
        public string Export(FinancialReportSummaryDto summary)
        {
            return "csv";
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

    private sealed class FakeProductCatalogService : IProductCatalogService
    {
        private readonly List<ProductServiceListItemDto> _items;

        public FakeProductCatalogService(IEnumerable<ProductServiceListItemDto>? seedItems = null)
        {
            _items = seedItems?.ToList() ?? [];
        }

        public int UpdateCalls { get; private set; }

        public Task<Guid> RegisterAsync(CreateProductServiceCommand command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(UpdateProductServiceCommand command, CancellationToken cancellationToken = default)
        {
            int index = _items.FindIndex(item => item.Id == command.Id);

            if (index < 0)
            {
                throw new InvalidOperationException("Produto/serviço informado não foi encontrado.");
            }

            _items[index] = new ProductServiceListItemDto(
                Id: command.Id,
                Name: command.Name,
                Category: command.Category,
                UnitPrice: command.UnitPrice,
                IsService: command.IsService,
                IsActive: command.IsActive);

            UpdateCalls++;
            return Task.CompletedTask;
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
