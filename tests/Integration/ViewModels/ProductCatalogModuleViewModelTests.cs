using SMEFinanceSuite.App.Desktop.ViewModels;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.ProductServices;
using Xunit;

namespace SMEFinanceSuite.Tests.Integration.ViewModels;

public sealed class ProductCatalogModuleViewModelTests
{
    [Fact]
    public async Task LoadAsync_ShouldExposeOnlyActiveItemsInActiveCollection()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var activeId = Guid.NewGuid();
        var inactiveId = Guid.NewGuid();

        var service = new FakeProductCatalogService(
            [
                new ProductServiceListItemDto(
                    Id: activeId,
                    Name: "Serviço Ativo",
                    Category: "Serviços",
                    UnitPrice: 500m,
                    IsService: true,
                    IsActive: true),
                new ProductServiceListItemDto(
                    Id: inactiveId,
                    Name: "Serviço Inativo",
                    Category: "Serviços",
                    UnitPrice: 700m,
                    IsService: true,
                    IsActive: false)
            ]);

        var viewModel = new ProductCatalogModuleViewModel(service);

        await viewModel.LoadAsync(cancellationToken);

        Assert.Equal(2, viewModel.ProductServices.Count);
        ProductServiceListItemViewModel activeItem = Assert.Single(viewModel.ActiveProductServices);
        Assert.Equal(activeId, activeItem.Id);
        Assert.DoesNotContain(viewModel.ActiveProductServices, item => item.Id == inactiveId);
    }

    [Fact]
    public async Task ToggleSelectedActiveAsync_ShouldKeepItemInCatalogAndUpdateActiveCollection()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var itemId = Guid.NewGuid();
        var service = new FakeProductCatalogService(
            [
                new ProductServiceListItemDto(
                    Id: itemId,
                    Name: "Item Alternável",
                    Category: "Serviços",
                    UnitPrice: 300m,
                    IsService: true,
                    IsActive: true)
            ]);

        var viewModel = new ProductCatalogModuleViewModel(service);
        await viewModel.LoadAsync(cancellationToken);
        viewModel.SelectedProductService = Assert.Single(viewModel.ProductServices);

        await viewModel.ToggleSelectedActiveAsync(cancellationToken);

        ProductServiceListItemViewModel inactivated = Assert.Single(viewModel.ProductServices);
        Assert.Equal(itemId, inactivated.Id);
        Assert.False(inactivated.IsActive);
        Assert.Empty(viewModel.ActiveProductServices);
        Assert.Equal(1, service.UpdateCalls);

        await viewModel.ToggleSelectedActiveAsync(cancellationToken);

        ProductServiceListItemViewModel activated = Assert.Single(viewModel.ProductServices);
        Assert.True(activated.IsActive);
        ProductServiceListItemViewModel activeItem = Assert.Single(viewModel.ActiveProductServices);
        Assert.Equal(itemId, activeItem.Id);
        Assert.Equal(2, service.UpdateCalls);
    }

    private sealed class FakeProductCatalogService(List<ProductServiceListItemDto> seedItems) : IProductCatalogService
    {
        private readonly List<ProductServiceListItemDto> _items = seedItems;

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
                throw new InvalidOperationException("Item não encontrado.");
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
