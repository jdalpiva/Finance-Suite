using SMEFinanceSuite.Core.Application.Reports;

namespace SMEFinanceSuite.Core.Application.Abstractions;

public interface IFinancialReportsService
{
    Task<FinancialReportSummaryDto> GetSummaryAsync(
        FinancialReportFilter? filter = null,
        CancellationToken cancellationToken = default);
}
