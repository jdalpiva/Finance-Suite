using System;
using System.Globalization;

class Program
{
    static void Main()
    {
        var ptBR = CultureInfo.GetCultureInfo("pt-BR");
        string input = "1.5";
        if (decimal.TryParse(input, NumberStyles.Number, ptBR, out decimal result))
        {
            Console.WriteLine($"Parsed '1.5' as pt-BR: {result}");
        }
        else
        {
            Console.WriteLine("pt-BR failed");
        }
        
        input = "1,5";
        if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result2))
        {
            Console.WriteLine($"Parsed '1,5' Invariant: {result2}");
        }
        else
        {
            Console.WriteLine("InvariantCulture failed for 1,5");
        }
    }
}
