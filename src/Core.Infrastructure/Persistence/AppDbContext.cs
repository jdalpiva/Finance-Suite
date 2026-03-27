using Microsoft.EntityFrameworkCore;
using SMEFinanceSuite.Core.Domain.Entities;

namespace SMEFinanceSuite.Core.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<ProductService> ProductsServices => Set<ProductService>();

    public DbSet<FinancialEntry> FinancialEntries => Set<FinancialEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
