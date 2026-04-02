using Avalonia;
using System.Runtime.InteropServices;

namespace SMEFinanceSuite.App.Desktop;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex) when (IsMissingGraphicalSession(ex))
        {
            Console.Error.WriteLine("Falha ao iniciar SME Finance Suite: nenhuma sessao grafica foi detectada.");
            Console.Error.WriteLine("Execute o app em um ambiente desktop com DISPLAY configurado e tente novamente.");
            Environment.ExitCode = 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Falha ao iniciar SME Finance Suite.");
            Console.Error.WriteLine(ex.ToString());
            Environment.ExitCode = 1;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }

    private static bool IsMissingGraphicalSession(Exception exception)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return false;
        }

        string diagnostic = exception.ToString();

        return diagnostic.Contains("XOpenDisplay failed", StringComparison.OrdinalIgnoreCase)
            || diagnostic.Contains("Unable to open display", StringComparison.OrdinalIgnoreCase);
    }
}
