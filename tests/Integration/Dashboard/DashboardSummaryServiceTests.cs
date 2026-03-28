using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SMEFinanceSuite.Core.Domain.Entities;
using SMEFinanceSuite.Core.Domain.Enums;
using SMEFinanceSuite.Core.Infrastructure.Persistence;
using SMEFinanceSuite.Core.Infrastructure.Services;
using Xunit;

namespace SMEFinanceSuite.Tests.Integration.Dashboard;

public sealed class DashboardSummaryServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_ShouldReturnAggregatedValues()
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

            var customer = new Customer("Cliente Integração");
            var service = new ProductService("Serviço de Teste", "Serviços", 100m, isService: true);

            setupContext.Customers.Add(customer);
            setupContext.ProductsServices.Add(service);
            setupContext.FinancialEntries.AddRange(
                new FinancialEntry("Receita", 500m, DateOnly.FromDateTime(DateTime.Today), EntryType.Revenue, customer.Id, service.Id),
                new FinancialEntry("Despesa", 150m, DateOnly.FromDateTime(DateTime.Today), EntryType.Expense));

            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var dbContextFactory = new TestDbContextFactory(options);
        var dashboardSummaryService = new DashboardSummaryService(dbContextFactory);

        var summary = await dashboardSummaryService.GetSummaryAsync(cancellationToken: cancellationToken);

        Assert.Equal(500m, summary.TotalRevenue);
        Assert.Equal(150m, summary.TotalExpense);
        Assert.Equal(350m, summary.NetCashFlow);
        Assert.Equal(1, summary.CustomersCount);
        Assert.Equal(1, summary.ProductsCount);
    }

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext()
        {
            return new AppDbContext(options);
        }

        public ValueTask<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new AppDbContext(options));
        }
    }
}
