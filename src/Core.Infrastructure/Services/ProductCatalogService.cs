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

        if (!command.IsActive)
        {
            productService.SetActive(false);
        }

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

    public async Task UpdateAsync(UpdateProductServiceCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ProductService? item = await dbContext.ProductsServices
            .FirstOrDefaultAsync(productService => productService.Id == command.Id, cancellationToken);

        if (item is null)
        {
            throw new InvalidOperationException("Produto/serviço informado não foi encontrado.");
        }

        item.Rename(command.Name);
        item.UpdateCategory(command.Category);
        item.UpdatePricing(command.UnitPrice);
        item.UpdateKind(command.IsService);
        item.SetActive(command.IsActive);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        bool hasLinkedEntries = await dbContext.FinancialEntries
            .AsNoTracking()
            .AnyAsync(entry => entry.ProductServiceId == id, cancellationToken);

        if (hasLinkedEntries)
        {
            throw new InvalidOperationException("Não é possível excluir um produto/serviço com lançamentos vinculados. Atualize ou remova os lançamentos antes da exclusão.");
        }

        ProductService? item = await dbContext.ProductsServices
            .FirstOrDefaultAsync(productService => productService.Id == id, cancellationToken);

        if (item is null)
        {
            throw new InvalidOperationException("Produto/serviço informado não foi encontrado.");
        }

        dbContext.ProductsServices.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
