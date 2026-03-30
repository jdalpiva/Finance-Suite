using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Infrastructure.Persistence;
using SMEFinanceSuite.Core.Infrastructure.Services;

namespace SMEFinanceSuite.Core.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<AppDbContext>(options =>
        {
            options.UseSqlite(connectionString);
            options.EnableSensitiveDataLogging(false);
        });

        services.AddScoped<IFinancialDashboardService, DashboardSummaryService>();
        services.AddScoped<IFinancialEntryService, FinancialEntryService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IApplicationInitializer, DatabaseInitializer>();

        return services;
    }
}
