using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SMEFinanceSuite.App.Bootstrapper;
using SMEFinanceSuite.App.Desktop.ViewModels;
using SMEFinanceSuite.App.Desktop.Views;

namespace SMEFinanceSuite.App.Desktop;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private IServiceScope? _uiScope;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            var configuration = ServiceRegistration.BuildConfiguration();
            var services = new ServiceCollection();
            services.AddSingleton(configuration);
            services.AddSmeFinanceSuite(configuration);

            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();
            desktopLifetime.Startup += OnStartupAsync;
            desktopLifetime.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void OnStartupAsync(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        if (_serviceProvider is null || ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            return;
        }

        try
        {
            await _serviceProvider.InitializeApplicationAsync();

            _uiScope = _serviceProvider.CreateScope();
            desktopLifetime.MainWindow = _uiScope.ServiceProvider.GetRequiredService<MainWindow>();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Falha na inicializacao da aplicacao: {exception}");
            desktopLifetime.Shutdown(-1);
        }
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            desktopLifetime.Startup -= OnStartupAsync;
            desktopLifetime.Exit -= OnExit;
        }

        _uiScope?.Dispose();
        _serviceProvider?.Dispose();
    }
}
