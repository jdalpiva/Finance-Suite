namespace SMEFinanceSuite.Core.Domain.Common;

public static class Guard
{
    public static string AgainstNullOrWhiteSpace(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("O valor informado é obrigatório.", parameterName);
        }

        string trimmed = value.Trim();

        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"O valor não pode exceder {maxLength} caracteres.", parameterName);
        }

        return trimmed;
    }

    public static decimal AgainstNegativeOrZero(decimal value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "O valor deve ser maior que zero.");
        }

        return value;
    }

    public static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();

        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"O valor não pode exceder {maxLength} caracteres.");
        }

        return trimmed;
    }
}
