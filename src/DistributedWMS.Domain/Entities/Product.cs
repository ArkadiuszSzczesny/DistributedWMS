using DistributedWMS.Domain.Common;
using DistributedWMS.Domain.Exceptions;

namespace DistributedWMS.Domain.Entities;

/// <summary>
/// Kartoteka towarowa. Definiuje czym jest produkt, natomiast informacja
/// o ilościach przechowywana jest wyłącznie w encji StockItem.
/// </summary>
public class Product : EntityBase
{
    private Product() { }

    public Product(string sku, string name, string unitOfMeasure, decimal minimumStock)
    {
        SetSku(sku);
        SetName(name);
        SetUnitOfMeasure(unitOfMeasure);
        SetMinimumStock(minimumStock);
        IsActive = true;
    }

    /// <summary>Indeks katalogowy - unikalny identyfikator handlowy towaru.</summary>
    public string Sku { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Description { get; set; }

    /// <summary>Jednostka miary, np. szt, m, kg.</summary>
    public string UnitOfMeasure { get; private set; } = "szt";

    /// <summary>
    /// Próg alarmowy wykorzystywany przez raport braków.
    /// Dotyczy łącznego stanu we wszystkich lokalizacjach.
    /// </summary>
    public decimal MinimumStock { get; private set; }

    /// <summary>Znacznik aktywności - towary wycofane nie są usuwane ze względu na historię dokumentów.</summary>
    public bool IsActive { get; private set; }

    public ICollection<StockItem> StockItems { get; private set; } = new List<StockItem>();

    public void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("Indeks towaru nie może być pusty.");
        Sku = sku.Trim().ToUpperInvariant();
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nazwa towaru nie może być pusta.");
        Name = name.Trim();
    }

    public void SetMinimumStock(decimal minimumStock)
    {
        if (minimumStock < 0)
            throw new DomainException("Stan minimalny nie może być ujemny.");
        MinimumStock = minimumStock;
    }
    
    public void SetUnitOfMeasure(string unitOfMeasure)
    {
        UnitOfMeasure = string.IsNullOrWhiteSpace(unitOfMeasure) ? "szt" : unitOfMeasure.Trim();
    }
    public void Deactivate() => IsActive = false;
}
