using SMEFinanceSuite.Core.Application.Reports;

namespace SMEFinanceSuite.Core.Application.Abstractions;

public interface IFinancialReportCsvExporter
{
    string Export(FinancialReportSummaryDto summary);
}
