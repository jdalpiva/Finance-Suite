using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SMEFinanceSuite.Core.Application.ProductServices;
using SMEFinanceSuite.Core.Domain.Entities;
using SMEFinanceSuite.Core.Infrastructure.Persistence;
using SMEFinanceSuite.Core.Infrastructure.Services;
using SMEFinanceSuite.Tests.Integration.TestHelpers;
using Xunit;

namespace SMEFinanceSuite.Tests.Integration.ProductServices;

public sealed class ProductCatalogServiceTests
{
    [Fact]
    public async Task RegisterAsync_ShouldPersistItem_WhenCommandIsValid()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(cancellationToken);
        }

        var service = new ProductCatalogService(new TestDbContextFactory(options));

        Guid itemId = await service.RegisterAsync(
            new CreateProductServiceCommand(
                Name: "Consultoria Fiscal",
                Category: "Serviços",
                UnitPrice: 1800m,
                IsService: true),
            cancellationToken);

        IReadOnlyList<ProductServiceListItemDto> items = await service.ListAsync(cancellationToken);
        ProductServiceListItemDto persistedItem = Assert.Single(items, item => item.Id == itemId);

        Assert.Equal("Consultoria Fiscal", persistedItem.Name);
        Assert.Equal("Serviços", persistedItem.Category);
        Assert.Equal(1800m, persistedItem.UnitPrice);
        Assert.True(persistedItem.IsService);
        Assert.True(persistedItem.IsActive);
    }

    [Fact]
    public async Task ListAsync_ShouldReturnItemsOrderedByName()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(cancellationToken);

            setupContext.ProductsServices.AddRange(
                new ProductService("Zeta Equipamento", "Equipamentos", 5000m, isService: false),
                new ProductService("Alpha Assessoria", "Serviços", 1200m, isService: true));

            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var service = new ProductCatalogService(new TestDbContextFactory(options));

        IReadOnlyList<ProductServiceListItemDto> items = await service.ListAsync(cancellationToken);

        Assert.Equal("Alpha Assessoria", items[0].Name);
        Assert.Equal("Zeta Equipamento", items[1].Name);
    }
}
