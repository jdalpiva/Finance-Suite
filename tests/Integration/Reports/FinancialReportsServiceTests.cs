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

    [Fact]
    public async Task GetSummaryAsync_ShouldApplyEntryTypeFilterToTotalsAndBreakdowns()
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

            var customer = new Customer("Cliente Tipo");
            var productService = new ProductService("Plano Tipo", "Assinatura", 300m, isService: true);

            setupContext.Customers.Add(customer);
            setupContext.ProductsServices.Add(productService);
            setupContext.FinancialEntries.AddRange(
                new FinancialEntry("Receita março", 700m, new DateOnly(2026, 3, 5), EntryType.Revenue, customer.Id, productService.Id),
                new FinancialEntry("Despesa março", 250m, new DateOnly(2026, 3, 8), EntryType.Expense, customer.Id, productService.Id),
                new FinancialEntry("Receita abril", 300m, new DateOnly(2026, 4, 2), EntryType.Revenue));

            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var service = new FinancialReportsService(new TestDbContextFactory(options));

        FinancialReportSummaryDto summary = await service.GetSummaryAsync(
            new FinancialReportFilter(EntryType: EntryType.Expense),
            cancellationToken);

        Assert.Equal(0m, summary.TotalRevenue);
        Assert.Equal(250m, summary.TotalExpense);
        Assert.Equal(-250m, summary.NetBalance);

        FinancialReportMonthlyBreakdownItemDto march = Assert.Single(summary.BreakdownByMonth);
        Assert.Equal(2026, march.Year);
        Assert.Equal(3, march.Month);
        Assert.Equal(0m, march.TotalRevenue);
        Assert.Equal(250m, march.TotalExpense);
        Assert.Equal(-250m, march.NetBalance);

        FinancialReportBreakdownItemDto customerItem = Assert.Single(summary.BreakdownByCustomer);
        Assert.Equal("Cliente Tipo", customerItem.Label);
        Assert.Equal(0m, customerItem.TotalRevenue);
        Assert.Equal(250m, customerItem.TotalExpense);
        Assert.Equal(-250m, customerItem.NetBalance);

        FinancialReportBreakdownItemDto productItem = Assert.Single(summary.BreakdownByProductService);
        Assert.Equal("Plano Tipo", productItem.Label);
        Assert.Equal(0m, productItem.TotalRevenue);
        Assert.Equal(250m, productItem.TotalExpense);
        Assert.Equal(-250m, productItem.NetBalance);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldCalculateComparisonUsingPreviousPeriodWithSameDuration()
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
                new FinancialEntry("Receita anterior", 400m, new DateOnly(2026, 3, 6), EntryType.Revenue),
                new FinancialEntry("Despesa anterior", 100m, new DateOnly(2026, 3, 7), EntryType.Expense),
                new FinancialEntry("Receita atual", 700m, new DateOnly(2026, 3, 11), EntryType.Revenue),
                new FinancialEntry("Despesa atual", 250m, new DateOnly(2026, 3, 13), EntryType.Expense),
                new FinancialEntry("Fora do comparativo", 999m, new DateOnly(2026, 3, 20), EntryType.Revenue));

            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var service = new FinancialReportsService(new TestDbContextFactory(options));

        FinancialReportSummaryDto summary = await service.GetSummaryAsync(
            new FinancialReportFilter(From: new DateOnly(2026, 3, 10), To: new DateOnly(2026, 3, 14)),
            cancellationToken);

        Assert.Equal(700m, summary.TotalRevenue);
        Assert.Equal(250m, summary.TotalExpense);
        Assert.Equal(450m, summary.NetBalance);

        FinancialReportPeriodComparisonDto comparison = Assert.IsType<FinancialReportPeriodComparisonDto>(summary.PeriodComparison);
        Assert.Equal(new DateOnly(2026, 3, 10), comparison.CurrentFrom);
        Assert.Equal(new DateOnly(2026, 3, 14), comparison.CurrentTo);
        Assert.Equal(new DateOnly(2026, 3, 5), comparison.PreviousFrom);
        Assert.Equal(new DateOnly(2026, 3, 9), comparison.PreviousTo);
        Assert.Equal(400m, comparison.PreviousTotalRevenue);
        Assert.Equal(100m, comparison.PreviousTotalExpense);
        Assert.Equal(300m, comparison.PreviousNetBalance);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldApplyEntryTypeFilterToComparison()
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
                new FinancialEntry("Receita anterior", 300m, new DateOnly(2026, 3, 6), EntryType.Revenue),
                new FinancialEntry("Despesa anterior", 90m, new DateOnly(2026, 3, 7), EntryType.Expense),
                new FinancialEntry("Receita atual", 500m, new DateOnly(2026, 3, 11), EntryType.Revenue),
                new FinancialEntry("Despesa atual", 150m, new DateOnly(2026, 3, 12), EntryType.Expense));

            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var service = new FinancialReportsService(new TestDbContextFactory(options));

        FinancialReportSummaryDto summary = await service.GetSummaryAsync(
            new FinancialReportFilter(
                From: new DateOnly(2026, 3, 10),
                To: new DateOnly(2026, 3, 14),
                EntryType: EntryType.Revenue),
            cancellationToken);

        Assert.Equal(500m, summary.TotalRevenue);
        Assert.Equal(0m, summary.TotalExpense);
        Assert.Equal(500m, summary.NetBalance);

        FinancialReportPeriodComparisonDto comparison = Assert.IsType<FinancialReportPeriodComparisonDto>(summary.PeriodComparison);
        Assert.Equal(300m, comparison.PreviousTotalRevenue);
        Assert.Equal(0m, comparison.PreviousTotalExpense);
        Assert.Equal(300m, comparison.PreviousNetBalance);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldOrderBreakdownsPredictably()
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

            var customerA = new Customer("Cliente A");
            var customerB = new Customer("Cliente B");
            var productA = new ProductService("Serviço A", "Serviços", 100m, isService: true);
            var productB = new ProductService("Serviço B", "Serviços", 100m, isService: true);

            setupContext.Customers.AddRange(customerA, customerB);
            setupContext.ProductsServices.AddRange(productA, productB);
            setupContext.FinancialEntries.AddRange(
                new FinancialEntry("B receita", 700m, new DateOnly(2026, 4, 10), EntryType.Revenue, customerB.Id, productB.Id),
                new FinancialEntry("A receita", 1000m, new DateOnly(2026, 3, 15), EntryType.Revenue, customerA.Id, productA.Id),
                new FinancialEntry("A despesa", 900m, new DateOnly(2026, 3, 20), EntryType.Expense, customerA.Id, productA.Id),
                new FinancialEntry("B despesa menor", 100m, new DateOnly(2026, 4, 12), EntryType.Expense, customerB.Id, productB.Id));

            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var service = new FinancialReportsService(new TestDbContextFactory(options));

        FinancialReportSummaryDto summary = await service.GetSummaryAsync(cancellationToken: cancellationToken);

        Assert.Collection(
            summary.BreakdownByMonth,
            first =>
            {
                Assert.Equal(2026, first.Year);
                Assert.Equal(3, first.Month);
            },
            second =>
            {
                Assert.Equal(2026, second.Year);
                Assert.Equal(4, second.Month);
            });

        Assert.Equal("Cliente A", summary.BreakdownByCustomer[0].Label);
        Assert.Equal("Cliente B", summary.BreakdownByCustomer[1].Label);

        Assert.Equal("Serviço A", summary.BreakdownByProductService[0].Label);
        Assert.Equal("Serviço B", summary.BreakdownByProductService[1].Label);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldKeepBreakdownOrderingWithEntryTypeFilter()
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

            var customerA = new Customer("Cliente A");
            var customerB = new Customer("Cliente B");
            var productA = new ProductService("Serviço A", "Serviços", 100m, isService: true);
            var productB = new ProductService("Serviço B", "Serviços", 100m, isService: true);

            setupContext.Customers.AddRange(customerA, customerB);
            setupContext.ProductsServices.AddRange(productA, productB);
            setupContext.FinancialEntries.AddRange(
                new FinancialEntry("A receita", 900m, new DateOnly(2026, 3, 15), EntryType.Revenue, customerA.Id, productA.Id),
                new FinancialEntry("A despesa", 800m, new DateOnly(2026, 3, 20), EntryType.Expense, customerA.Id, productA.Id),
                new FinancialEntry("B receita", 700m, new DateOnly(2026, 4, 10), EntryType.Revenue, customerB.Id, productB.Id),
                new FinancialEntry("B despesa", 100m, new DateOnly(2026, 4, 12), EntryType.Expense, customerB.Id, productB.Id));

            await setupContext.SaveChangesAsync(cancellationToken);
        }

        var service = new FinancialReportsService(new TestDbContextFactory(options));

        FinancialReportSummaryDto summary = await service.GetSummaryAsync(
            new FinancialReportFilter(EntryType: EntryType.Expense),
            cancellationToken);

        Assert.Equal("Cliente A", summary.BreakdownByCustomer[0].Label);
        Assert.Equal("Cliente B", summary.BreakdownByCustomer[1].Label);
        Assert.Equal("Serviço A", summary.BreakdownByProductService[0].Label);
        Assert.Equal("Serviço B", summary.BreakdownByProductService[1].Label);
    }
}
