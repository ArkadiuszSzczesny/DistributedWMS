using DistributedWMS.Domain.Common;
using DistributedWMS.Domain.Exceptions;

namespace DistributedWMS.Domain.Entities;

/// <summary>Pozycja dokumentu magazynowego - pojedynczy towar wraz z ilością.</summary>
public class DocumentLine : EntityBase
{
    private DocumentLine() { }

    public DocumentLine(Guid productId, decimal quantity, decimal? unitPrice = null)
    {
        if (quantity <= 0)
            throw new DomainException("Ilość w pozycji dokumentu musi być większa od zera.");
        if(unitPrice is < 0)
            throw new DomainException("Cena jednostkowa nie może być ujemna.");

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid DocumentId { get; private set; }
    public Document Document { get; private set; } = null!;

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;

    public decimal Quantity { get; private set; }

    /// <summary>Cena jednostkowa - wypełniana przy przyjęciu zewnętrznym, opcjonalna dla pozostałych typów.</summary>
    public decimal? UnitPrice { get; private set; }

    public void ChangeQuantity(decimal quantity)
    {
        if(quantity <= 0)
            throw new DomainException("Ilość w pozycji dokumentu musi być większa od zera.");
        Quantity = quantity;
    }
}