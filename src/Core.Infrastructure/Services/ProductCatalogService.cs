using Microsoft.EntityFrameworkCore;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.ProductServices;
using SMEFinanceSuite.Core.Domain.Entities;
using SMEFinanceSuite.Core.Infrastructure.Persistence;

namespace SMEFinanceSuite.Core.Infrastructure.Services;

public sealed class ProductCatalogService(IDbContextFactory<AppDbContext> dbContextFactory) : IProductCatalogService
{
    public async Task<Guid> RegisterAsync(CreateProductServiceCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ProductService productService = new(
            name: command.Name,
            category: command.Category,
            unitPrice: command.UnitPrice,
            isService: command.IsService);

        dbContext.ProductsServices.Add(productService);
        await dbContext.SaveChangesAsync(cancellationToken);

        return productService.Id;
    }

    public async Task<IReadOnlyList<ProductServiceListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<ProductServiceListItemDto> items = await dbContext.ProductsServices
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Category)
            .Take(200)
            .Select(item => new ProductServiceListItemDto(
                Id: item.Id,
                Name: item.Name,
                Category: item.Category,
                UnitPrice: item.UnitPrice,
                IsService: item.IsService,
                IsActive: item.IsActive))
            .ToListAsync(cancellationToken);

        return items;
    }
}
