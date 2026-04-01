using System.Globalization;

namespace SMEFinanceSuite.App.Desktop.ViewModels;

internal static class DecimalInputParser
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");

    public static bool TryParse(string rawValue, out decimal value)
    {
        if (decimal.TryParse(rawValue, NumberStyles.Number, PortugueseCulture, out value))
        {
            return true;
        }

        return decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
