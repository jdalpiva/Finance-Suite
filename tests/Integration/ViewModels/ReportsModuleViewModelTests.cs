using SMEFinanceSuite.App.Desktop.ViewModels;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.Reports;
using SMEFinanceSuite.Core.Domain.Enums;
using Xunit;

namespace SMEFinanceSuite.Tests.Integration.ViewModels;

public sealed class ReportsModuleViewModelTests
{
    [Fact]
    public async Task ApplyFiltersAsync_ShouldRequestSummaryAndPopulateBreakdowns()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var service = new FakeReportsService();
        var viewModel = new ReportsModuleViewModel(service, new FakeReportCsvExporter())
        {
            FilterFrom = "2026-03-01",
            FilterTo = "2026-03-31"
        };

        await viewModel.ApplyFiltersAsync(cancellationToken);

        Assert.Equal(new DateOnly(2026, 3, 1), service.LastFilter?.From);
        Assert.Equal(new DateOnly(2026, 3, 31), service.LastFilter?.To);
        Assert.Null(service.LastFilter?.EntryType);
        Assert.Contains("1.200,00", viewModel.TotalRevenueDisplay);
        Assert.Contains("350,00", viewModel.TotalExpenseDisplay);
        FinancialReportMonthlyBreakdownItemViewModel monthly = Assert.Single(viewModel.BreakdownByMonth);
        Assert.Equal("03/2026", monthly.MonthDisplay);
        Assert.Single(viewModel.BreakdownByCustomer);
        Assert.Single(viewModel.BreakdownByProductService);
    }

    [Fact]
    public async Task ApplyFiltersAsync_ShouldSendSelectedEntryType()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var service = new FakeReportsService();
        var viewModel = new ReportsModuleViewModel(service, new FakeReportCsvExporter())
        {
            SelectedFilterType = "Despesa"
        };

        await viewModel.ApplyFiltersAsync(cancellationToken);

        Assert.Equal(EntryType.Expense, service.LastFilter?.EntryType);
    }

    [Fact]
    public async Task CreateCsvExport_ShouldUseCurrentSummaryAndSuggestedFileName()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var service = new FakeReportsService();
        var exporter = new FakeReportCsvExporter();
        var viewModel = new ReportsModuleViewModel(service, exporter)
        {
            FilterFrom = "2026-03-01",
            FilterTo = "2026-03-31"
        };

        await viewModel.LoadAsync(cancellationToken);

        FinancialReportCsvExport export = viewModel.CreateCsvExport();

        Assert.Equal("relatorio-financeiro-2026-03-01-a-2026-03-31.csv", export.SuggestedFileName);
        Assert.Equal("csv-gerado", export.Content);
        Assert.NotNull(exporter.LastSummary);
        Assert.Equal(1200m, exporter.LastSummary!.TotalRevenue);
        Assert.Equal(350m, exporter.LastSummary.TotalExpense);
    }

    [Fact]
    public async Task LoadAsync_ShouldExposeComparisonDisplays_WhenPeriodComparisonIsAvailable()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var service = new FakeReportsService();
        var viewModel = new ReportsModuleViewModel(service, new FakeReportCsvExporter())
        {
            FilterFrom = "2026-03-10",
            FilterTo = "2026-03-14"
        };

        await viewModel.LoadAsync(cancellationToken);

        Assert.True(viewModel.HasPeriodComparison);
        Assert.Contains("Período atual (selecionado): 10/03/2026 até 14/03/2026", viewModel.PeriodComparisonDisplay);
        Assert.Contains("Período anterior equivalente: 05/03/2026 até 09/03/2026", viewModel.PeriodComparisonDisplay);
        Assert.Contains("variação absoluta", viewModel.RevenueComparisonDisplay);
        Assert.Contains("variação percentual", viewModel.RevenueComparisonDisplay);
        Assert.Contains("variação absoluta", viewModel.ExpenseComparisonDisplay);
        Assert.Contains("variação percentual", viewModel.ExpenseComparisonDisplay);
        Assert.Contains("variação absoluta", viewModel.NetBalanceComparisonDisplay);
        Assert.Contains("variação percentual", viewModel.NetBalanceComparisonDisplay);
    }

    [Fact]
    public async Task LoadAsync_ShouldExposeComparisonAsUnavailable_WhenPeriodComparisonIsMissing()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var service = new FakeReportsService();
        var viewModel = new ReportsModuleViewModel(service, new FakeReportCsvExporter());

        await viewModel.LoadAsync(cancellationToken);

        Assert.False(viewModel.HasPeriodComparison);
        Assert.Contains("Comparativo indisponível", viewModel.PeriodComparisonDisplay);
    }

    private sealed class FakeReportsService : IFinancialReportsService
    {
        public FinancialReportFilter? LastFilter { get; private set; }

        public Task<FinancialReportSummaryDto> GetSummaryAsync(FinancialReportFilter? filter = null, CancellationToken cancellationToken = default)
        {
            LastFilter = filter;

            FinancialReportPeriodComparisonDto? periodComparison = null;
            DateOnly? from = filter?.From;
            DateOnly? to = filter?.To;

            if (from is not null && to is not null)
            {
                int currentPeriodLength = to.Value.DayNumber - from.Value.DayNumber + 1;
                DateOnly previousTo = from.Value.AddDays(-1);
                DateOnly previousFrom = previousTo.AddDays(-(currentPeriodLength - 1));

                periodComparison = new FinancialReportPeriodComparisonDto(
                    CurrentFrom: from.Value,
                    CurrentTo: to.Value,
                    PreviousFrom: previousFrom,
                    PreviousTo: previousTo,
                    PreviousTotalRevenue: 1000m,
                    PreviousTotalExpense: 200m,
                    PreviousNetBalance: 800m);
            }

            FinancialReportSummaryDto summary = new(
                From: filter?.From,
                To: filter?.To,
                TotalRevenue: 1200m,
                TotalExpense: 350m,
                NetBalance: 850m,
                BreakdownByMonth:
                [
                    new FinancialReportMonthlyBreakdownItemDto(
                        Year: 2026,
                        Month: 3,
                        TotalRevenue: 1200m,
                        TotalExpense: 350m,
                        NetBalance: 850m)
                ],
                BreakdownByCustomer:
                [
                    new FinancialReportBreakdownItemDto(
                        ReferenceId: Guid.NewGuid(),
                        Label: "Cliente Exemplo",
                        Category: null,
                        TotalRevenue: 1200m,
                        TotalExpense: 0m,
                        NetBalance: 1200m)
                ],
                BreakdownByProductService:
                [
                    new FinancialReportBreakdownItemDto(
                        ReferenceId: Guid.NewGuid(),
                        Label: "Serviço Exemplo",
                        Category: "Serviços",
                        TotalRevenue: 1200m,
                        TotalExpense: 0m,
                        NetBalance: 1200m)
                ],
                PeriodComparison: periodComparison);

            return Task.FromResult(summary);
        }
    }

    private sealed class FakeReportCsvExporter : IFinancialReportCsvExporter
    {
        public FinancialReportSummaryDto? LastSummary { get; private set; }

        public string Export(FinancialReportSummaryDto summary)
        {
            LastSummary = summary;
            return "csv-gerado";
        }
    }
}
