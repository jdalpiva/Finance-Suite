namespace SMEFinanceSuite.Core.Application.ProductServices;

public sealed record UpdateProductServiceCommand(
    Guid Id,
    string Name,
    string Category,
    decimal UnitPrice,
    bool IsService,
    bool IsActive);
