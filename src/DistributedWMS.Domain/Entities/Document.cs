using DistributedWMS.Domain.Common;
using DistributedWMS.Domain.Enums;
using DistributedWMS.Domain.Exceptions;

namespace DistributedWMS.Domain.Entities;

///<summary>
///Dokument magazynowy stanowiący jedyne źródło zmian stanów magazynowych.
///Dokument roboczy nie wpływa na stany; zaksięgowanie następuje w ramach
///pojedynczej transakcji bazodanowej.
///</summary>
public class Document : EntityBase
{
    private readonly List<DocumentLine> _lines = new();

    private Document() { }

    public Document(
        string number,
        DocumentType type,
        Guid createdByUserId,
        Guid? sourceLocationId,
        Guid? targetLocationId,
        string? reference = null)
    {
        if(string.IsNullOrWhiteSpace(number))
            throw new DomainException("Numer dokumentu nie może być pusty.");
        
        number = number.Trim();
        Type = type;
        Status = DocumentStatus.Draft;
        CreatedByUserId = createdByUserId;
        SourceLocationId = sourceLocationId;
        TargetLocationId = targetLocationId;
        Reference = reference;
        DocumentDate = DateTime.UtcNow;

        ValidateLocations();
    }

    ///<summary>Numer dokumentu w formacie PZ/2026/0001.</summary>
    public string Number { get; private set; } = null!;

    public DocumentType Type { get; private set; }

    public DocumentStatus Status { get; private set; }

    public DateTime DocumentDate { get; private set; }

    public DateTime? PostedAtUtc { get; private set; }

    ///<summary>Lokalizacja źródłowa - rozchód. Pusta dla przyjęcia zewnętrznego.</summary>
    public Guid? SourceLocationId { get; private set; }
    public Location? SourceLocation { get; private set; }

    ///<summary>Lokalizacja docelowa - przychód. Pista dla rozchodu wewnętrznego.</summary>
    public Guid? TargetLocationId { get; private set; }
    public Location? TargetLocation { get; private set; }

    public Guid CreatedByUserId { get; private set; }
    public User CreatedByUser { get; private set; } = null!;

    ///<summary>Odnośnik zewnętrzny - numer faktury dostawcy lub numer zlecenia serwisowego.</summary>
    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public IReadOnlyCollection<DocumentLine> Lines => _lines.AsReadOnly();

    public bool IsEditable => Status == DocumentStatus.Draft;

    public void AddLine(Guid productId, decimal quantity, decimal? unitPrice = null)
    {
        EnsureEditable();

        var existing = _lines.FirstOrDefault(l => l.ProductId == productId);
        if(existing is not null)
        {
            existing.ChangeQuantity(existing.Quantity + quantity);
            return;
        }

        _lines.Add(new DocumentLine(productId, quantity, unitPrice));
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureEditable();

        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new DomainException("Wskazana pozycja nie istnieje w dokumencie.");

        _lines.Remove(line);
    }

    ///<summary>
    ///Oznacza dokument jako zaksięgowany. Wywoływane przez warstwę aplikacji
    ///po pomyślnej aktualizacji stanów magazynowych w ramach  transakcji.
    ///</summary>
    public void MarkAsPosted()
    {
        EnsureEditable();

        if (_lines.Count == 0)
            throw new DomainException("Nie można zatwierdzić dokumentu bez pozycji.");

        Status = DocumentStatus.Posted;
        PostedAtUtc = DateTime.UtcNow;
        ModifiedAtUtc = DateTime.UtcNow;
    }   

    public void Cancel()
    {
        if (Status == DocumentStatus.Cancelled)
            throw new DomainException("Dokument został już anulowany.");

        Status = DocumentStatus.Cancelled;
        ModifiedAtUtc = DateTime.UtcNow;
    }

    private void EnsureEditable()
    {
        if (!IsEditable)
            throw new DomainException(
                $"Dokument {Number} ma status {Status} i nie może być modyfikowany.");
    }

    ///<summary>Weryfikuje zgodność lokalizacji z typem dokumentu.</summary>
    private void ValidateLocations()
    {
        switch (Type)
        {
            case DocumentType.GoodsReceipt:
                if (TargetLocationId is null)
                    throw new DomainException("Przyjęcie zewnętrzne wymaga lokalizacji docelowej.");
                if (SourceLocationId is not null)
                    throw new DomainException("Przyjęcie zewnętrzne nie posiada lokalizacji źródłowej.");
                break;

            case DocumentType.StockTransfer:
                if (SourceLocationId is null || TargetLocationId is null)
                    throw new DomainException("Przesunięcie wymaga lokalizacji źródłowej i docelowej.");
                if (SourceLocationId == TargetLocationId)
                    throw new DomainException("Lokalizacja źródłowa i docelowa muszą być różne");
                break;

            case DocumentType.InternalIssue:
                if (SourceLocationId is null)
                    throw new DomainException("Rozchód wewnętrzny wymaga lokalizacji źródłowej.");
                if (TargetLocationId is not null)
                    throw new DomainException("Rozchód wewnętrzny nie posiada lokaliazacji docelowej.");
                break;

            case DocumentType.Inventory:
                if (TargetLocationId is null)
                    throw new DomainException("Inwentaryzacja wymaga wskazania lokalizacji.");
                break;
        }
    }
}

