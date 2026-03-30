namespace SMEFinanceSuite.Core.Application.ProductServices;

public sealed record ProductServiceListItemDto(
    Guid Id,
    string Name,
    string Category,
    decimal UnitPrice,
    bool IsService,
    bool IsActive);
