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
                Name: "Cliente Sprint 4",
                Email: "contato@sprint4.com",
                Phone: "41999990000"),
            cancellationToken);

        IReadOnlyList<CustomerListItemDto> customers = await service.ListAsync(cancellationToken);
        CustomerListItemDto persistedCustomer = Assert.Single(customers, item => item.Id == customerId);

        Assert.Equal("Cliente Sprint 4", persistedCustomer.Name);
        Assert.Equal("contato@sprint4.com", persistedCustomer.Email);
        Assert.Equal("41999990000", persistedCustomer.Phone);
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

    [Fact]
    public async Task UpdateAsync_ShouldPersistNameAndContact_WhenCustomerExists()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        Guid customerId;

        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(cancellationToken);

            var customer = new Customer("Cliente Original", "original@empresa.com", "41999998888");
            setupContext.Customers.Add(customer);
            await setupContext.SaveChangesAsync(cancellationToken);

            customerId = customer.Id;
        }

        var service = new CustomerService(new TestDbContextFactory(options));

        await service.UpdateAsync(
            new UpdateCustomerCommand(
                Id: customerId,
                Name: "Cliente Atualizado",
                Email: "atualizado@empresa.com",
                Phone: "41911112222"),
            cancellationToken);

        IReadOnlyList<CustomerListItemDto> customers = await service.ListAsync(cancellationToken);
        CustomerListItemDto updatedCustomer = Assert.Single(customers);

        Assert.Equal(customerId, updatedCustomer.Id);
        Assert.Equal("Cliente Atualizado", updatedCustomer.Name);
        Assert.Equal("atualizado@empresa.com", updatedCustomer.Email);
        Assert.Equal("41911112222", updatedCustomer.Phone);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveCustomer_WhenCustomerExists()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        Guid customerId;

        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync(cancellationToken);

            var customer = new Customer("Cliente para excluir");
            setupContext.Customers.Add(customer);
            await setupContext.SaveChangesAsync(cancellationToken);

            customerId = customer.Id;
        }

        var service = new CustomerService(new TestDbContextFactory(options));

        await service.DeleteAsync(customerId, cancellationToken);

        IReadOnlyList<CustomerListItemDto> customers = await service.ListAsync(cancellationToken);
        Assert.Empty(customers);
    }
}
