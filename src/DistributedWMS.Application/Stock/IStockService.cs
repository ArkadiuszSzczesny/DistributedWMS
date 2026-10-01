using DistributedWMS.Shared.Stock;

namespace DistributedWMS.Application.Stock;

public interface IStockService
{
    /// <summary>Stany wszystkich towarów w danej lokalizacji.</summary>
    Task<IReadOnlyList<StockItemDto>> GetByLocationAsync(Guid locationId, bool skipZero = true, CancellationToken ct = default);

    /// <summary>Rozmieszczenie jednego towaru we wszystkich lokalizacjach.</summary>
    Task<IReadOnlyList<StockItemDto>> GetByProductAsync(Guid productId, CancellationToken ct = default);

    /// <summary>Raport braków - towary o stanie łącznym poniżej progu minimalnego.</summary>
    Task<IReadOnlyList<StockSummaryDto>> GetBelowMinimumAsync(CancellationToken ct = default);
}