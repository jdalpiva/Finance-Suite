namespace SMEFinanceSuite.Core.Application.Abstractions;

public interface IApplicationInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
