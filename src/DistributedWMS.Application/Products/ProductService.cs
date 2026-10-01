using DistributedWMS.Application.Common.Exceptions;
using DistributedWMS.Application.Common.Interfaces;
using DistributedWMS.Domain.Entities;
using DistributedWMS.Domain.Exceptions;
using DistributedWMS.Shared.Products;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Runtime.CompilerServices;

namespace DistributedWMS.Application.Products;

public class ProductService : IProductService
{
    private readonly IWmsDbContext _db;

    public ProductService(IWmsDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(bool onlyActive = true, CancellationToken ct = default)
    {
        var query = _db.Products.AsNoTracking();

        if (onlyActive)
            query = query.Where(p => p.IsActive);


        return await query
            .OrderBy(p => p.Sku)
            .Select(p => new ProductDto(
                p.Id, p.Sku, p.Name, p.Description, p.UnitOfMeasure, p.MinimumStock, p.IsActive))
            .ToListAsync(ct);
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Towar", id);

        return Map(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var sku = request.sku.Trim().ToUpperInvariant();

        var exists = await _db.Products.AnyAsync(p => p.Sku == sku, ct);
        if (exists)
            throw new DomainException($"Towar z kodem SKU '{sku}' już istnieje.");

        var product = new Product(request.sku, request.Name, request.UnitOfMeasure, request.MinimumStock)
        {
            Description = request.Description
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        return Map(product);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Towar", id);

        product.SetName(request.Name);
        product.SetMinimumStock(request.MinimumStock);
        product.Description = request.Description;
        product.ModifiedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Map(product);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("Towar", id);

        var hasStock = await _db.StockItems.AnyAsync(s => s.ProductId == id && s.Quantity > 0, ct);
        if (hasStock)
            throw new DomainException("Nie można wycofać towaru z istniejącym stanem magazynowym.");

        product.Deactivate();
        await _db.SaveChangesAsync(ct);
    }

    private static ProductDto Map(Product p) => new(
        p.Id, p.Sku, p.Name, p.Description, p.UnitOfMeasure, p.MinimumStock, p.IsActive);
}


  