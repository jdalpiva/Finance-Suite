using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SMEFinanceSuite.App.Bootstrapper;
using SMEFinanceSuite.App.Desktop.ViewModels;
using SMEFinanceSuite.App.Desktop.Views;
using SMEFinanceSuite.Core.Infrastructure.Services;

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

            using (IServiceScope setupScope = _serviceProvider.CreateScope())
            {
                DatabaseInitializer databaseInitializer = setupScope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
                databaseInitializer.InitializeAsync().GetAwaiter().GetResult();
            }

            _uiScope = _serviceProvider.CreateScope();
            desktopLifetime.MainWindow = _uiScope.ServiceProvider.GetRequiredService<MainWindow>();
            desktopLifetime.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _uiScope?.Dispose();
        _serviceProvider?.Dispose();
    }
}
