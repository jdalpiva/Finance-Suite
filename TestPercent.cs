using System;
using System.Globalization;
class Program
{
    static void Main()
    {
        var ptBR = CultureInfo.GetCultureInfo("pt-BR");
        decimal val1 = 0.5m;
        decimal val2 = -0.051m;
        Console.WriteLine(val1.ToString("+0.00%;-0.00%;0.00%", ptBR));
        Console.WriteLine(val2.ToString("+0.00%;-0.00%;0.00%", ptBR));
    }
}
