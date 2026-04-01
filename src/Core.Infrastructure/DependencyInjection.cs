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

        // Em Avalonia desktop, a aplicacao usa um unico scope de UI durante o ciclo da janela principal.
        // Na pratica, serviços Scoped resolvidos nesse scope se comportam como singletons de sessao.
        services.AddScoped<IFinancialDashboardService, DashboardSummaryService>();
        services.AddScoped<IFinancialReportsService, FinancialReportsService>();
        services.AddScoped<IFinancialReportCsvExporter, FinancialReportCsvExporter>();
        services.AddScoped<IFinancialEntryService, FinancialEntryService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProductCatalogService, ProductCatalogService>();
        services.AddScoped<IApplicationInitializer, DatabaseInitializer>();

        return services;
    }
}
