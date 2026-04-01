using SMEFinanceSuite.Core.Application.Reports;
using SMEFinanceSuite.Core.Infrastructure.Services;
using Xunit;

namespace SMEFinanceSuite.Tests.Integration.Reports;

public sealed class FinancialReportCsvExporterTests
{
    [Fact]
    public void Export_ShouldGenerateReadableSectionsForSummaryAndBreakdowns()
    {
        var exporter = new FinancialReportCsvExporter();
        FinancialReportSummaryDto summary = new(
            From: new DateOnly(2026, 3, 1),
            To: new DateOnly(2026, 3, 31),
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

        string csv = exporter.Export(summary);

        Assert.Contains("Resumo", csv);
        Assert.Contains("Campo;Valor", csv);
        Assert.Contains("Período de;2026-03-01", csv);
        Assert.Contains("Período até;2026-03-31", csv);
        Assert.Contains("Receitas;1200,00", csv);
        Assert.Contains("Despesas;350,00", csv);
        Assert.Contains("Saldo;850,00", csv);
        Assert.Contains("Breakdown mensal", csv);
        Assert.Contains("Mês;Receitas;Despesas;Saldo", csv);
        Assert.Contains("2026-03;1200,00;350,00;850,00", csv);
        Assert.Contains("Breakdown por cliente", csv);
        Assert.Contains("Cliente Exemplo;1200,00;0,00;1200,00", csv);
        Assert.Contains("Breakdown por produto/serviço", csv);
        Assert.Contains("Serviço Exemplo;Serviços;1200,00;0,00;1200,00", csv);
    }

    [Fact]
    public void Export_ShouldEscapeSeparatorAndQuotesInTextFields()
    {
        var exporter = new FinancialReportCsvExporter();
        FinancialReportSummaryDto summary = new(
            From: null,
            To: null,
            TotalRevenue: 15m,
            TotalExpense: 0m,
            NetBalance: 15m,
            BreakdownByMonth:
            [
                new FinancialReportMonthlyBreakdownItemDto(
                    Year: 2026,
                    Month: 4,
                    TotalRevenue: 15m,
                    TotalExpense: 0m,
                    NetBalance: 15m)
            ],
            BreakdownByCustomer:
            [
                new FinancialReportBreakdownItemDto(
                    ReferenceId: Guid.NewGuid(),
                    Label: "Cliente \"A;B\"",
                    Category: null,
                    TotalRevenue: 10m,
                    TotalExpense: 0m,
                    NetBalance: 10m)
            ],
            BreakdownByProductService:
            [
                new FinancialReportBreakdownItemDto(
                    ReferenceId: Guid.NewGuid(),
                    Label: "Pacote;Premium",
                    Category: "Nível \"1\"",
                    TotalRevenue: 5m,
                    TotalExpense: 0m,
                    NetBalance: 5m)
            ]);

        string csv = exporter.Export(summary);

        Assert.Contains("\"Cliente \"\"A;B\"\"\";10,00;0,00;10,00", csv);
        Assert.Contains("\"Pacote;Premium\";\"Nível \"\"1\"\"\";5,00;0,00;5,00", csv);
    }

    [Fact]
    public void Export_ShouldIncludeComparisonSection_WhenComparisonIsAvailable()
    {
        var exporter = new FinancialReportCsvExporter();
        FinancialReportSummaryDto summary = new(
            From: new DateOnly(2026, 3, 10),
            To: new DateOnly(2026, 3, 14),
            TotalRevenue: 700m,
            TotalExpense: 250m,
            NetBalance: 450m,
            BreakdownByMonth: [],
            BreakdownByCustomer: [],
            BreakdownByProductService: [],
            PeriodComparison: new FinancialReportPeriodComparisonDto(
                CurrentFrom: new DateOnly(2026, 3, 10),
                CurrentTo: new DateOnly(2026, 3, 14),
                PreviousFrom: new DateOnly(2026, 3, 5),
                PreviousTo: new DateOnly(2026, 3, 9),
                PreviousTotalRevenue: 400m,
                PreviousTotalExpense: 100m,
                PreviousNetBalance: 300m));

        string csv = exporter.Export(summary);

        Assert.Contains("Comparativo entre períodos", csv);
        Assert.Contains("Métrica;Período atual;Período anterior;Variação absoluta;Variação percentual", csv);
        Assert.Contains("Período;2026-03-10 até 2026-03-14;2026-03-05 até 2026-03-09;-;-", csv);
        Assert.Contains("Receita;700,00;400,00;+300,00;+75,00%", csv);
        Assert.Contains("Despesa;250,00;100,00;+150,00;+150,00%", csv);
        Assert.Contains("Saldo;450,00;300,00;+150,00;+50,00%", csv);
    }

    [Fact]
    public void Export_ShouldShowComparisonAsUnavailable_WhenComparisonIsNull()
    {
        var exporter = new FinancialReportCsvExporter();
        FinancialReportSummaryDto summary = new(
            From: new DateOnly(2026, 3, 1),
            To: new DateOnly(2026, 3, 31),
            TotalRevenue: 1200m,
            TotalExpense: 350m,
            NetBalance: 850m,
            BreakdownByMonth: [],
            BreakdownByCustomer: [],
            BreakdownByProductService: [],
            PeriodComparison: null);

        string csv = exporter.Export(summary);

        Assert.Contains("Comparativo entre períodos", csv);
        Assert.Contains("Status;Comparativo indisponível. Defina data inicial e final do período.", csv);
    }
}
