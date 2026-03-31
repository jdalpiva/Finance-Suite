using SMEFinanceSuite.App.Desktop.ViewModels;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.Reports;
using Xunit;

namespace SMEFinanceSuite.Tests.Integration.ViewModels;

public sealed class ReportsModuleViewModelTests
{
    [Fact]
    public async Task ApplyFiltersAsync_ShouldRequestSummaryAndPopulateBreakdowns()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var service = new FakeReportsService();
        var viewModel = new ReportsModuleViewModel(service)
        {
            FilterFrom = "2026-03-01",
            FilterTo = "2026-03-31"
        };

        await viewModel.ApplyFiltersAsync(cancellationToken);

        Assert.Equal(new DateOnly(2026, 3, 1), service.LastFilter?.From);
        Assert.Equal(new DateOnly(2026, 3, 31), service.LastFilter?.To);
        Assert.Contains("1.200,00", viewModel.TotalRevenueDisplay);
        Assert.Contains("350,00", viewModel.TotalExpenseDisplay);
        Assert.Single(viewModel.BreakdownByCustomer);
        Assert.Single(viewModel.BreakdownByProductService);
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
}
