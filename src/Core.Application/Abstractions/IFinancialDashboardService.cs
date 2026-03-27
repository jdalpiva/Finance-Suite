using SMEFinanceSuite.Core.Application.Dashboard;

namespace SMEFinanceSuite.Core.Application.Abstractions;

public interface IFinancialDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);
}
