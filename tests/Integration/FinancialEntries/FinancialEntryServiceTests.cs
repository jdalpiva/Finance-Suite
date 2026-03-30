using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SMEFinanceSuite.Core.Application.FinancialEntries;
using SMEFinanceSuite.Core.Domain.Entities;
using SMEFinanceSuite.Core.Domain.Enums;
using SMEFinanceSuite.Core.Infrastructure.Persistence;
using SMEFinanceSuite.Core.Infrastructure.Services;
using SMEFinanceSuite.Tests.Integration.TestHelpers;
using Xunit;

namespace SMEFinanceSuite.Tests.Integration.FinancialEntries;

public sealed class FinancialEntryServiceTests
{
    [Fact]
    public async Task RegisterAsync_ShouldPersistEntry_WhenReferencesAreValid()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        Guid customerId;
        Guid productServiceId;

        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(cancellationToken);

            var customer = new Customer("Cliente Integração");
            var productService = new ProductService("Consultoria", "Serviços", 500m, isService: true);

            setupContext.Customers.Add(customer);
            setupContext.ProductsServices.Add(productService);
            await setupContext.SaveChangesAsync(cancellationToken);

            customerId = customer.Id;
            productServiceId = productService.Id;
        }

        var service = new FinancialEntryService(new TestDbContextFactory(options));

        Guid entryId = await service.RegisterAsync(
            new CreateFinancialEntryCommand(
                Description: "Receita de consultoria",
                Amount: 1500.50m,
                OccurredOn: DateOnly.FromDateTime(DateTime.Today),
                EntryType: EntryType.Revenue,
                CustomerId: customerId,
                ProductServiceId: productServiceId,
                Notes: "Contrato mensal"),
            cancellationToken);

        IReadOnlyList<FinancialEntryListItemDto> entries = await service.ListAsync(cancellationToken: cancellationToken);

        FinancialEntryListItemDto entry = Assert.Single(entries, item => item.Id == entryId);
        Assert.Equal(1500.50m, entry.Amount);
        Assert.Equal(EntryType.Revenue, entry.EntryType);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrow_WhenCustomerReferenceDoesNotExist()
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

        var service = new FinancialEntryService(new TestDbContextFactory(options));

        var command = new CreateFinancialEntryCommand(
            Description: "Receita inválida",
            Amount: 100m,
            OccurredOn: DateOnly.FromDateTime(DateTime.Today),
            EntryType: EntryType.Revenue,
            CustomerId: Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(command, cancellationToken));
    }

    [Fact]
    public async Task ListAsync_ShouldApplyPeriodAndTypeFilters()
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

            setupContext.FinancialEntries.AddRange(
                new FinancialEntry("Receita A", 100m, DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), EntryType.Revenue),
                new FinancialEntry("Despesa B", 40m, DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), EntryType.Expense),
                new FinancialEntry("Receita C", 70m, DateOnly.FromDateTime(DateTime.Today), EntryType.Revenue));

            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var service = new FinancialEntryService(new TestDbContextFactory(options));

        IReadOnlyList<FinancialEntryListItemDto> filtered = await service.ListAsync(
            new FinancialEntriesFilter(
                From: DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                To: DateOnly.FromDateTime(DateTime.Today),
                EntryType: EntryType.Revenue),
            cancellationToken);

        Assert.Single(filtered);
        Assert.Equal("Receita C", filtered[0].Description);
    }
}
