using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SMEFinanceSuite.Core.Application.Customers;
using SMEFinanceSuite.Core.Domain.Entities;
using SMEFinanceSuite.Core.Infrastructure.Persistence;
using SMEFinanceSuite.Core.Infrastructure.Services;
using SMEFinanceSuite.Tests.Integration.TestHelpers;
using Xunit;

namespace SMEFinanceSuite.Tests.Integration.Customers;

public sealed class CustomerServiceTests
{
    [Fact]
    public async Task RegisterAsync_ShouldPersistCustomer_WhenCommandIsValid()
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

        var service = new CustomerService(new TestDbContextFactory(options));

        Guid customerId = await service.RegisterAsync(
            new CreateCustomerCommand(
                Name: "Cliente Sprint 4"),
            cancellationToken);

        IReadOnlyList<CustomerListItemDto> customers = await service.ListAsync(cancellationToken);
        CustomerListItemDto persistedCustomer = Assert.Single(customers, item => item.Id == customerId);

        Assert.Equal("Cliente Sprint 4", persistedCustomer.Name);
    }

    [Fact]
    public async Task ListAsync_ShouldReturnCustomersOrderedByName()
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

            setupContext.Customers.AddRange(
                new Customer("Zulu Ltda."),
                new Customer("Alfa Tecnologia"));

            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var service = new CustomerService(new TestDbContextFactory(options));

        IReadOnlyList<CustomerListItemDto> customers = await service.ListAsync(cancellationToken);

        Assert.Equal("Alfa Tecnologia", customers[0].Name);
        Assert.Equal("Zulu Ltda.", customers[1].Name);
    }
}
