using DistributedWMS.Application.Common.Interfaces;
using DistributedWMS.Shared.Stock;
using Microsoft.EntityFrameworkCore;

namespace DistributedWMS.Application.Stock;

public class StockService : IStockService
{
    private readonly IWmsDbContext _db;

    public StockService(IWmsDbContext db) => _db = db;

    public async Task<IReadOnlyList<StockItemDto>> GetByLocationAsync(
        Guid locationId, bool skipZero = true, CancellationToken ct = default)
    {
        var query = _db.StockItems
            .AsNoTracking()
            .Where(s => s.LocationId == locationId);

        if (skipZero)
            query = query.Where(s => s.Quantity > 0);

        return await query
            .OrderBy(s => s.Product.Sku)
            .Select(s => new StockItemDto(
                s.ProductId,
                s.Product.Sku,
                s.Product.Name,
                s.Product.UnitOfMeasure,
                s.Location.Code,
                s.Location.Name,
                s.Quantity))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StockItemDto>> GetByProductAsync(Guid productId, CancellationToken ct = default)
    {
        return await _db.StockItems
            .AsNoTracking()
            .Where(s => s.ProductId == productId && s.Quantity > 0)
            .OrderBy(s => s.Location.Type)
            .ThenBy(s => s.Location.Code)
            .Select(s => new StockItemDto(
                s.ProductId,
                s.Product.Sku,
                s.Product.Name,
                s.Product.UnitOfMeasure,
                s.Location.Code,
                s.Location.Name,
                s.Quantity))
            .ToListAsync(ct);
    }
    public async Task<IReadOnlyList<StockSummaryDto>> GetBelowMinimumAsync(CancellationToken ct = default)
    {
        // Sumowanie stanów ze wszystkich lokalizacji wykonywane jest po stronie bazy danych.
        var rows = await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.MinimumStock > 0)
            .Select(p => new
            {
                p.Id,
                p.Sku,
                p.Name,
                p.UnitOfMeasure,
                p.MinimumStock,
                Total = _db.StockItems
                    .Where(s => s.ProductId == p.Id)
                    .Sum(s => (decimal?)s.Quantity) ?? 0m
            })
            .Where(x => x.Total < x.MinimumStock)
            .OrderBy(x => x.Sku)
            .ToListAsync(ct);

        return rows
            .Select(x => new StockSummaryDto(
                x.Id, x.Sku, x.Name, x.UnitOfMeasure, x.MinimumStock, x.Total))
            .ToList();
    }
}