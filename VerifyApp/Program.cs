using System;
using System.Globalization;

class Program
{
    static void Main()
    {
        var ptBR = CultureInfo.GetCultureInfo("pt-BR");
        string input = "1500.50";
        if (decimal.TryParse(input, NumberStyles.Number, ptBR, out decimal result))
        {
            Console.WriteLine($"Parsed as: {result}");
        }
        else
        {
            Console.WriteLine("Failed pt-BR");
        }
    }
}
