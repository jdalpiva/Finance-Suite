using Microsoft.EntityFrameworkCore;
using SMEFinanceSuite.Core.Infrastructure.Persistence;

namespace SMEFinanceSuite.Tests.Integration.TestHelpers;

internal sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext()
    {
        return new AppDbContext(options);
    }

    public ValueTask<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new AppDbContext(options));
    }
}
