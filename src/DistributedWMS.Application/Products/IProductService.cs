using DistributedWMS.Shared.Products;

namespace DistributedWMS.Application.Products;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(bool onlyActive = true, CancellationToken ct = default);
    Task<ProductDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, CancellationToken ct = default);
}
