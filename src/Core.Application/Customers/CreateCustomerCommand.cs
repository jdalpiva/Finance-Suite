namespace SMEFinanceSuite.Core.Application.Customers;

public sealed record CreateCustomerCommand(
    string Name,
    string? Email = null,
    string? Phone = null);
