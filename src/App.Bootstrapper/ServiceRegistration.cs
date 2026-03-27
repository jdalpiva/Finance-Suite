using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SMEFinanceSuite.Core.Infrastructure;

namespace SMEFinanceSuite.App.Bootstrapper;

public static class ServiceRegistration
{
    public static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    public static IServiceCollection AddSmeFinanceSuite(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=sme-finance-suite.db";

        services.AddInfrastructure(connectionString);

        return services;
    }
}
