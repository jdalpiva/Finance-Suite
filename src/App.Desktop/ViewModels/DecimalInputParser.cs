using System.Globalization;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

internal static class DecimalInputParser
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly NumberStyles NumberStyles = System.Globalization.NumberStyles.Number;

    public static bool TryParse(string rawValue, out decimal value)
    {
        value = 0m;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        string input = rawValue.Trim();
        bool hasComma = input.Contains(',');
        bool hasDot = input.Contains('.');

        if (hasComma && hasDot)
        {
            return ParseMixedSeparators(input, out value);
        }

        if (hasDot)
        {
            if (LooksLikeGroupedThousands(input))
            {
                if (decimal.TryParse(input, NumberStyles, PortugueseCulture, out value))
                {
                    return true;
                }
            }

            if (decimal.TryParse(input, NumberStyles, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            return decimal.TryParse(input, NumberStyles, PortugueseCulture, out value);
        }

        if (decimal.TryParse(input, NumberStyles, PortugueseCulture, out value))
        {
            return true;
        }

        return decimal.TryParse(input, NumberStyles, CultureInfo.InvariantCulture, out value);
    }

    private static bool ParseMixedSeparators(string input, out decimal value)
    {
        int lastCommaIndex = input.LastIndexOf(',');
        int lastDotIndex = input.LastIndexOf('.');

        if (lastCommaIndex > lastDotIndex)
        {
            // Ex.: 1.500,50 -> remove separadores de milhar e normaliza para parsing invariant.
            string normalized = input
                .Replace(".", string.Empty, StringComparison.Ordinal)
                .Replace(",", ".", StringComparison.Ordinal);

            return decimal.TryParse(normalized, NumberStyles, CultureInfo.InvariantCulture, out value);
        }

        // Ex.: 1,500.50 -> remove separadores de milhar e mantém ponto decimal.
        string normalizedDot = input.Replace(",", string.Empty, StringComparison.Ordinal);
        return decimal.TryParse(normalizedDot, NumberStyles, CultureInfo.InvariantCulture, out value);
    }

    private static bool LooksLikeGroupedThousands(string input)
    {
        string unsigned = input;

        if (unsigned.StartsWith("+", StringComparison.Ordinal) || unsigned.StartsWith("-", StringComparison.Ordinal))
        {
            unsigned = unsigned[1..];
        }

        string[] groups = unsigned.Split('.');

        if (groups.Length < 2 || groups[0].Length is < 1 or > 3 || !IsDigitsOnly(groups[0]))
        {
            return false;
        }

        for (int index = 1; index < groups.Length; index++)
        {
            if (groups[index].Length != 3 || !IsDigitsOnly(groups[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsDigitsOnly(string value)
    {
        foreach (char current in value)
        {
            if (!char.IsDigit(current))
            {
                return false;
            }
        }

        return true;
    }
}
