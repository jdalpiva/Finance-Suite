using SMEFinanceSuite.Core.Application.ProductServices;

namespace SMEFinanceSuite.Core.Application.Abstractions;

public interface IProductCatalogService
{
    Task<Guid> RegisterAsync(CreateProductServiceCommand command, CancellationToken cancellationToken = default);

    Task UpdateAsync(UpdateProductServiceCommand command, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductServiceListItemDto>> ListAsync(CancellationToken cancellationToken = default);
}
