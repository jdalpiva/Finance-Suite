using SMEFinanceSuite.Core.Application.ProductServices;

namespace SMEFinanceSuite.Core.Application.Abstractions;

public interface IProductCatalogService
{
    Task<Guid> RegisterAsync(CreateProductServiceCommand command, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductServiceListItemDto>> ListAsync(CancellationToken cancellationToken = default);
}
