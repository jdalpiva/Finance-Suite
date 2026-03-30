using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Infrastructure;

namespace SMEFinanceSuite.App.Bootstrapper;

public static class ServiceRegistration
{
    private const string SqliteDataSourceKey = "Data Source";

    public static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    public static IServiceCollection AddSmeFinanceSuite(this IServiceCollection services, IConfiguration configuration)
    {
        string configuredConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=sme-finance-suite.db";

        string resolvedConnectionString = ResolveConnectionString(configuredConnectionString);

        services.AddInfrastructure(resolvedConnectionString);

        return services;
    }

    public static async Task InitializeApplicationAsync(this IServiceProvider rootServiceProvider, CancellationToken cancellationToken = default)
    {
        using IServiceScope startupScope = rootServiceProvider.CreateScope();
        IApplicationInitializer applicationInitializer = startupScope.ServiceProvider.GetRequiredService<IApplicationInitializer>();
        await applicationInitializer.InitializeAsync(cancellationToken);
    }

    private static string ResolveConnectionString(string connectionString)
    {
        DbConnectionStringBuilder builder = new()
        {
            ConnectionString = connectionString
        };

        if (!builder.TryGetValue(SqliteDataSourceKey, out object? dataSourceValue))
        {
            return connectionString;
        }

        string dataSource = Convert.ToString(dataSourceValue)?.Trim() ?? string.Empty;

        if (dataSource.Length == 0
            || dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            || Path.IsPathRooted(dataSource))
        {
            return connectionString;
        }

        string appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SMEFinanceSuite");

        Directory.CreateDirectory(appDataDirectory);

        builder[SqliteDataSourceKey] = Path.Combine(appDataDirectory, dataSource);

        return builder.ConnectionString;
    }
}
