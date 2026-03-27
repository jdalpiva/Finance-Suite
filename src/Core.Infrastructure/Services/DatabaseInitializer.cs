using Microsoft.EntityFrameworkCore;
using SMEFinanceSuite.Core.Domain.Entities;
using SMEFinanceSuite.Core.Domain.Enums;
using SMEFinanceSuite.Core.Infrastructure.Persistence;

namespace SMEFinanceSuite.Core.Infrastructure.Services;

public sealed class DatabaseInitializer(IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (await dbContext.Customers.AnyAsync(cancellationToken))
        {
            return;
        }

        Customer customer = new("Cliente Exemplo", "cliente@exemplo.com", "(41) 99999-0000");
        ProductService consulting = new("Consultoria Financeira", "Serviços", 2500m, isService: true);
        ProductService maintenance = new("Manutenção de Equipamentos", "Serviços", 800m, isService: true);

        dbContext.Customers.Add(customer);
        dbContext.ProductsServices.AddRange(consulting, maintenance);

        dbContext.FinancialEntries.AddRange(
            new FinancialEntry(
                description: "Recebimento de consultoria",
                amount: 4500m,
                occurredOn: DateOnly.FromDateTime(DateTime.Today.AddDays(-7)),
                entryType: EntryType.Revenue,
                customerId: customer.Id,
                productServiceId: consulting.Id,
                notes: "Seed inicial"),
            new FinancialEntry(
                description: "Recebimento de manutenção",
                amount: 1800m,
                occurredOn: DateOnly.FromDateTime(DateTime.Today.AddDays(-3)),
                entryType: EntryType.Revenue,
                customerId: customer.Id,
                productServiceId: maintenance.Id,
                notes: "Seed inicial"),
            new FinancialEntry(
                description: "Pagamento de aluguel",
                amount: 1200m,
                occurredOn: DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
                entryType: EntryType.Expense,
                notes: "Seed inicial"),
            new FinancialEntry(
                description: "Pagamento de energia",
                amount: 380m,
                occurredOn: DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),
                entryType: EntryType.Expense,
                notes: "Seed inicial"));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
