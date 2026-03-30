namespace SMEFinanceSuite.Core.Application.Customers;

public sealed record CustomerListItemDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone);
