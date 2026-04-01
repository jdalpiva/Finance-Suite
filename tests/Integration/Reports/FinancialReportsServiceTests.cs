using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SMEFinanceSuite.Core.Application.Reports;
using SMEFinanceSuite.Core.Domain.Entities;
using SMEFinanceSuite.Core.Domain.Enums;
using SMEFinanceSuite.Core.Infrastructure.Persistence;
using SMEFinanceSuite.Core.Infrastructure.Services;
using SMEFinanceSuite.Tests.Integration.TestHelpers;
using Xunit;

namespace SMEFinanceSuite.Tests.Integration.Reports;

public sealed class FinancialReportsServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_ShouldReturnTotalsAndBreakdowns()
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

            var customerA = new Customer("Cliente Alfa");
            var customerB = new Customer("Cliente Beta");
            var productA = new ProductService("Consultoria", "Serviços", 300m, isService: true);
            var productB = new ProductService("Licença", "Software", 500m, isService: false);

            setupContext.Customers.AddRange(customerA, customerB);
            setupContext.ProductsServices.AddRange(productA, productB);
            setupContext.FinancialEntries.AddRange(
                new FinancialEntry("Receita A", 1000m, new DateOnly(2026, 3, 1), EntryType.Revenue, customerA.Id, productA.Id),
                new FinancialEntry("Despesa A", 200m, new DateOnly(2026, 3, 2), EntryType.Expense, customerA.Id, productA.Id),
                new FinancialEntry("Receita B", 500m, new DateOnly(2026, 3, 5), EntryType.Revenue, customerB.Id, productB.Id),
                new FinancialEntry("Despesa sem vínculo", 100m, new DateOnly(2026, 3, 10), EntryType.Expense),
                new FinancialEntry("Receita sem cliente", 150m, new DateOnly(2026, 4, 1), EntryType.Revenue, productServiceId: productA.Id));

            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var service = new FinancialReportsService(new TestDbContextFactory(options));

        FinancialReportSummaryDto summary = await service.GetSummaryAsync(cancellationToken: cancellationToken);

        Assert.Equal(1650m, summary.TotalRevenue);
        Assert.Equal(300m, summary.TotalExpense);
        Assert.Equal(1350m, summary.NetBalance);

        FinancialReportMonthlyBreakdownItemDto march = Assert.Single(
            summary.BreakdownByMonth,
            item => item.Year == 2026 && item.Month == 3);
        Assert.Equal(1500m, march.TotalRevenue);
        Assert.Equal(300m, march.TotalExpense);
        Assert.Equal(1200m, march.NetBalance);

        FinancialReportMonthlyBreakdownItemDto april = Assert.Single(
            summary.BreakdownByMonth,
            item => item.Year == 2026 && item.Month == 4);
        Assert.Equal(150m, april.TotalRevenue);
        Assert.Equal(0m, april.TotalExpense);
        Assert.Equal(150m, april.NetBalance);

        FinancialReportBreakdownItemDto customerAItem = Assert.Single(summary.BreakdownByCustomer, item => item.Label == "Cliente Alfa");
        Assert.Equal(1000m, customerAItem.TotalRevenue);
        Assert.Equal(200m, customerAItem.TotalExpense);

        FinancialReportBreakdownItemDto noCustomerItem = Assert.Single(summary.BreakdownByCustomer, item => item.Label == "Sem cliente");
        Assert.Equal(150m, noCustomerItem.TotalRevenue);
        Assert.Equal(100m, noCustomerItem.TotalExpense);

        FinancialReportBreakdownItemDto productAItem = Assert.Single(summary.BreakdownByProductService, item => item.Label == "Consultoria");
        Assert.Equal("Serviços", productAItem.Category);
        Assert.Equal(1150m, productAItem.TotalRevenue);
        Assert.Equal(200m, productAItem.TotalExpense);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldApplyPeriodFilter()
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
                new FinancialEntry("Receita período", 900m, new DateOnly(2026, 3, 3), EntryType.Revenue),
                new FinancialEntry("Despesa período", 250m, new DateOnly(2026, 3, 8), EntryType.Expense),
                new FinancialEntry("Receita fora", 400m, new DateOnly(2026, 4, 3), EntryType.Revenue));

            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var service = new FinancialReportsService(new TestDbContextFactory(options));

        FinancialReportSummaryDto summary = await service.GetSummaryAsync(
            new FinancialReportFilter(From: new DateOnly(2026, 3, 1), To: new DateOnly(2026, 3, 31)),
            cancellationToken);

        Assert.Equal(900m, summary.TotalRevenue);
        Assert.Equal(250m, summary.TotalExpense);
        Assert.Equal(650m, summary.NetBalance);
        FinancialReportMonthlyBreakdownItemDto march = Assert.Single(summary.BreakdownByMonth);
        Assert.Equal(2026, march.Year);
        Assert.Equal(3, march.Month);
        Assert.Equal(900m, march.TotalRevenue);
        Assert.Equal(250m, march.TotalExpense);
    }
}
