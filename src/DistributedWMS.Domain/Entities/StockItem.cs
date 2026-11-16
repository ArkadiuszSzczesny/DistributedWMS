using DistributedWMS.Domain.Common;
using DistributedWMS.Domain.Exceptions;

namespace DistributedWMS.Domain.Entities;

/// <summary>
/// Stan magazynowy - ilość konkretnego towaru w konkretnej lokalizacji.
/// Encja nigdy nie jest modyfikowana bezpośrednio przez użytkownika;
/// zmiany następują wyłącznie w wyniku zatwierdzania dokumentu magazynowego.
/// </summary>
public class StockItem : EntityBase
{
    private StockItem() { }

    public StockItem(Guid productId, Guid locationId, decimal quantity = 0m)
    {
        if(quantity < 0)
            throw new DomainException("Stan początkowy nie może być ujemny.");

        ProductId = productId;
        LocationId = locationId;
        Quantity = quantity;
    }

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;

    public Guid LocationId { get; private set; }
    public Location Location { get; private set; } = null!;

    public decimal Quantity { get; private set; }

    /// <summary>
    /// Znacznik wersji wiersza wykorzystywany do blokady optymistycznej.
    /// Mapowany na systemową kolumnę xmin PostgreSQL (konfiguracja w warstwie Infrastructure).
    /// </summary>
    public uint RowVersion { get; private set; }

    /// <summary>Zwiększa stan magazynowy. Stosowane przy przyjęciu oraz przy odbiorze przesunięcia.</summary>
    public void Increase(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Ilość zwiększenia stanu musi być większa od zera.");
        
        Quantity += quantity;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Zmniejsza stan magazynowy. Uniemożliwia powstanie stanu ujemnego,
    /// realizując wymaganie odporności systemu na błędy w stanach magazynowych.
    /// </summary>
    public void Decrease(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Ilość zmniejszenia stanu musi być większa od zera.");

        if (Quantity < quantity)
            throw new DomainException(
                $"Niewystarczający stan magazynowy. Dostępne: {Quantity}, żądane: {quantity}.");

        Quantity -= quantity;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Ustawia stan wprost - wyłącznie na potrzeby inwentaryzacji.</summary>
    public void SetQuantity(decimal quantity)
    {
        if (quantity < 0)
            throw new DomainException("Stan po inwentaryzacji nie może być ujemny.");

        Quantity = quantity;
        ModifiedAtUtc = DateTime.UtcNow;
    }

}
 