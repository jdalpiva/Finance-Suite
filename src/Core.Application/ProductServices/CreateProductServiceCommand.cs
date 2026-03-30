namespace SMEFinanceSuite.Core.Application.ProductServices;

public sealed record CreateProductServiceCommand(
    string Name,
    string Category,
    decimal UnitPrice,
    bool IsService);
