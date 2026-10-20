namespace DistributedWMS.Shared.Products;

/// <summary>Reprezentacja towaru przekazywana do klienta.</summary>
public record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    string UnitOfMeasure,
    decimal MinimumStock,
    bool IsActive);

public record CreateProductRequest(
    string Sku,
    string Name,
    string UnitOfMeasure,
    decimal MinimumStock,
    string? Description);

public record UpdateProductRequest(
    string Name,
    string UnitOfMeasure,
    decimal MinimumStock,
    string? Description);