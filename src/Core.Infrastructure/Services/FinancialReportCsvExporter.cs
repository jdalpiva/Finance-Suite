using System.Globalization;
using System.Text;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.Reports;

namespace SMEFinanceSuite.Core.Infrastructure.Services;

public sealed class FinancialReportCsvExporter : IFinancialReportCsvExporter
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");
    private const char CsvSeparator = ';';

    public string Export(FinancialReportSummaryDto summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var builder = new StringBuilder();

        WriteSectionTitle(builder, "Resumo");
        WriteRow(builder, "Campo", "Valor");
        WriteRow(builder, "Período de", FormatDate(summary.From));
        WriteRow(builder, "Período até", FormatDate(summary.To));
        WriteRow(builder, "Receitas", FormatAmount(summary.TotalRevenue));
        WriteRow(builder, "Despesas", FormatAmount(summary.TotalExpense));
        WriteRow(builder, "Saldo", FormatAmount(summary.NetBalance));
        WriteBlankLine(builder);

        WriteSectionTitle(builder, "Breakdown mensal");
        WriteRow(builder, "Mês", "Receitas", "Despesas", "Saldo");
        foreach (FinancialReportMonthlyBreakdownItemDto item in summary.BreakdownByMonth)
        {
            string monthDisplay = new DateOnly(item.Year, item.Month, 1).ToString("yyyy-MM", CultureInfo.InvariantCulture);
            WriteRow(builder, monthDisplay, FormatAmount(item.TotalRevenue), FormatAmount(item.TotalExpense), FormatAmount(item.NetBalance));
        }

        WriteBlankLine(builder);

        WriteSectionTitle(builder, "Breakdown por cliente");
        WriteRow(builder, "Cliente", "Receitas", "Despesas", "Saldo");
        foreach (FinancialReportBreakdownItemDto item in summary.BreakdownByCustomer)
        {
            WriteRow(builder, item.Label, FormatAmount(item.TotalRevenue), FormatAmount(item.TotalExpense), FormatAmount(item.NetBalance));
        }

        WriteBlankLine(builder);

        WriteSectionTitle(builder, "Breakdown por produto/serviço");
        WriteRow(builder, "Produto/Serviço", "Categoria", "Receitas", "Despesas", "Saldo");
        foreach (FinancialReportBreakdownItemDto item in summary.BreakdownByProductService)
        {
            WriteRow(
                builder,
                item.Label,
                item.Category ?? "-",
                FormatAmount(item.TotalRevenue),
                FormatAmount(item.TotalExpense),
                FormatAmount(item.NetBalance));
        }

        return builder.ToString();
    }

    private static void WriteSectionTitle(StringBuilder builder, string title)
    {
        builder.AppendLine(Escape(title));
    }

    private static void WriteRow(StringBuilder builder, params string[] values)
    {
        for (int index = 0; index < values.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(CsvSeparator);
            }

            builder.Append(Escape(values[index]));
        }

        builder.AppendLine();
    }

    private static void WriteBlankLine(StringBuilder builder)
    {
        builder.AppendLine();
    }

    private static string FormatDate(DateOnly? value)
    {
        return value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
    }

    private static string FormatAmount(decimal amount)
    {
        return amount.ToString("0.00", PortugueseCulture);
    }

    private static string Escape(string value)
    {
        bool requiresEscaping = value.Contains(CsvSeparator)
            || value.Contains('"')
            || value.Contains('\n')
            || value.Contains('\r');

        if (!requiresEscaping)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
