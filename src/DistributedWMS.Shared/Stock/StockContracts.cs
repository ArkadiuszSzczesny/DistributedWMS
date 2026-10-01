namespace DistributedWMS.Shared.Stock;

/// <summary>Stan pojedynczego towaru w konkretnej lokalizacji.</summary>
public record StockItemDto(
    Guid ProductId,
    string Sku,
    string ProductName,
    string UnitOfMeasure,
    Guid LocationId,
    string LocationCode,
    string LocationName,
    decimal Quantity);

/// <summary> 
/// Zbiorczy stan towaru we wszystkich lokalizacjach wraz z progiem alarmowym.
/// Wykorzystywany w raporcie braków.
/// </summary>
public record StockSummaryDto(
    Guid ProductId,
    string Sku,
    string ProductName,
    string UnitOfMeasure,
    decimal MinimumStock,
    decimal TotalQuantity)

{
    public decimal Shortage => MinimumStock - TotalQuantity;
    public bool IsBelowMinimum => TotalQuantity < MinimumStock;
}