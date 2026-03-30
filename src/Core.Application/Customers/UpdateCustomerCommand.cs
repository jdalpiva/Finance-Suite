namespace SMEFinanceSuite.Core.Application.Customers;

public sealed record UpdateCustomerCommand(
    Guid Id,
    string Name,
    string? Email = null,
    string? Phone = null);
