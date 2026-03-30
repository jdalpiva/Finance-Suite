using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SMEFinanceSuite.Core.Application.ProductServices;
using SMEFinanceSuite.Core.Domain.Entities;
using SMEFinanceSuite.Core.Domain.Enums;
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

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges_WhenItemExists()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        Guid itemId;

        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(cancellationToken);

            var item = new ProductService("Plano Inicial", "Serviços", 500m, isService: true);
            setupContext.ProductsServices.Add(item);
            await setupContext.SaveChangesAsync(cancellationToken);
            itemId = item.Id;
        }

        var service = new ProductCatalogService(new TestDbContextFactory(options));

        await service.UpdateAsync(
            new UpdateProductServiceCommand(
                Id: itemId,
                Name: "Plano Atualizado",
                Category: "Software",
                UnitPrice: 750m,
                IsService: false,
                IsActive: false),
            cancellationToken);

        IReadOnlyList<ProductServiceListItemDto> items = await service.ListAsync(cancellationToken);
        ProductServiceListItemDto updatedItem = Assert.Single(items);

        Assert.Equal("Plano Atualizado", updatedItem.Name);
        Assert.Equal("Software", updatedItem.Category);
        Assert.Equal(750m, updatedItem.UnitPrice);
        Assert.False(updatedItem.IsService);
        Assert.False(updatedItem.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveItem_WhenItemExists()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        Guid itemId;

        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(cancellationToken);

            var item = new ProductService("Pacote para excluir", "Serviços", 300m, isService: true);
            setupContext.ProductsServices.Add(item);
            await setupContext.SaveChangesAsync(cancellationToken);
            itemId = item.Id;
        }

        var service = new ProductCatalogService(new TestDbContextFactory(options));

        await service.DeleteAsync(itemId, cancellationToken);

        IReadOnlyList<ProductServiceListItemDto> items = await service.ListAsync(cancellationToken);
        Assert.Empty(items);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenItemHasLinkedEntries()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        Guid itemId;

        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(cancellationToken);

            var item = new ProductService("Item vinculado", "Serviços", 980m, isService: true);
            setupContext.ProductsServices.Add(item);
            await setupContext.SaveChangesAsync(cancellationToken);
            itemId = item.Id;

            var entry = new FinancialEntry(
                description: "Receita vinculada",
                amount: 980m,
                occurredOn: DateOnly.FromDateTime(DateTime.Today),
                entryType: EntryType.Revenue,
                productServiceId: itemId);

            setupContext.FinancialEntries.Add(entry);
            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var service = new ProductCatalogService(new TestDbContextFactory(options));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(itemId, cancellationToken));

        Assert.Equal("Não é possível excluir um produto/serviço com lançamentos vinculados. Atualize ou remova os lançamentos antes da exclusão.", exception.Message);
    }

    [Fact]
    public async Task RegisterAsync_ShouldAllowCreatingInactiveItem_WhenRequested()
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
                Name: "Item Inativo",
                Category: "Serviços",
                UnitPrice: 100m,
                IsService: true,
                IsActive: false),
            cancellationToken);

        IReadOnlyList<ProductServiceListItemDto> items = await service.ListAsync(cancellationToken);
        ProductServiceListItemDto item = Assert.Single(items, current => current.Id == itemId);

        Assert.False(item.IsActive);
    }
}
