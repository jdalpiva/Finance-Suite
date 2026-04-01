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

    private sealed class FakeReportsService : IFinancialReportsService
    {
        public FinancialReportFilter? LastFilter { get; private set; }

        public Task<FinancialReportSummaryDto> GetSummaryAsync(FinancialReportFilter? filter = null, CancellationToken cancellationToken = default)
        {
            LastFilter = filter;

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
                ]);

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
