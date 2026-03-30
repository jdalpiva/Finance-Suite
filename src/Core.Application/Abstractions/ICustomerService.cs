using SMEFinanceSuite.Core.Application.Customers;

namespace SMEFinanceSuite.Core.Application.Abstractions;

public interface ICustomerService
{
    Task<Guid> RegisterAsync(CreateCustomerCommand command, CancellationToken cancellationToken = default);

    Task UpdateAsync(UpdateCustomerCommand command, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerListItemDto>> ListAsync(CancellationToken cancellationToken = default);
}
