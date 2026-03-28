using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Domain.Entities;
using SMEFinanceSuite.Core.Domain.Enums;
using SMEFinanceSuite.Core.Infrastructure.Persistence;

namespace SMEFinanceSuite.Core.Infrastructure.Services;

public sealed class DatabaseInitializer(IDbContextFactory<AppDbContext> dbContextFactory) : IApplicationInitializer
{
    private const string InitialMigrationId = "20260328135740_InitialCreate";
    private const string EfProductVersion = "10.0.5";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        await EnsureLegacyDatabaseBaselineAsync(dbContext, cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);

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

    private static async Task EnsureLegacyDatabaseBaselineAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        DbConnection connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        if (await HasMigrationHistoryAsync(connection, cancellationToken))
        {
            return;
        }

        if (await CountLegacyTablesAsync(connection, cancellationToken) == 0)
        {
            return;
        }

        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        await ExecuteNonQueryAsync(
            connection,
            transaction,
            """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            """,
            cancellationToken);

        await ExecuteNonQueryAsync(
            connection,
            transaction,
            $"""
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            SELECT '{InitialMigrationId}', '{EfProductVersion}'
            WHERE NOT EXISTS (
                SELECT 1
                FROM "__EFMigrationsHistory"
                WHERE "MigrationId" = '{InitialMigrationId}'
            );
            """,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<bool> HasMigrationHistoryAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        const string query = """
            SELECT COUNT(1)
            FROM sqlite_master
            WHERE type = 'table' AND name = '__EFMigrationsHistory';
            """;

        return await ExecuteScalarAsIntAsync(connection, query, cancellationToken) > 0;
    }

    private static async Task<int> CountLegacyTablesAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        const string query = """
            SELECT COUNT(1)
            FROM sqlite_master
            WHERE type = 'table'
              AND name IN ('customers', 'products_services', 'financial_entries');
            """;

        return await ExecuteScalarAsIntAsync(connection, query, cancellationToken);
    }

    private static async Task<int> ExecuteScalarAsIntAsync(DbConnection connection, string query, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandType = CommandType.Text;

        object? scalar = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(scalar);
    }

    private static async Task ExecuteNonQueryAsync(
        DbConnection connection,
        DbTransaction transaction,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        command.CommandType = CommandType.Text;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
