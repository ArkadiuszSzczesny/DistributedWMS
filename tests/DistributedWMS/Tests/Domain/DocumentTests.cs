using DistributedWMS.Domain.Entities;
using DistributedWMS.Domain.Exceptions;
using DistributedWMS.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DistributedWMS.Tests.Domain;

/// <summary>
/// Weryfikuje zgodność lokalizacji z typem dokumentu oraz zasady
/// cyklu życia dokumentu magazynowego.
/// </summary>
public class DocumentTests
{
    private static readonly Guid User = Guid.NewGuid();
    private static readonly Guid Warehouse = Guid.NewGuid();
    private static readonly Guid Van = Guid.NewGuid();

    [Fact(DisplayName = "Przyjęcie zewnętrzne wymaga lokalizacji docelowej")]
    public void GoodsReceipt_RequiresTargetLocation()
    {
        var act = () => new Document("PZ/2026/0001", DocumentType.GoodsReceipt,
            User, sourceLocationId: null, targetLocationId: null);

        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Przyjęcie zewnętrzne nie dopuszcza lokalizacji źródłowej")]
    public void GoodsReceipt_RejectsSourceLocation()
    {
        var act = () => new Document("PZ/2026/0001", DocumentType.GoodsReceipt,
            User, sourceLocationId: Warehouse, targetLocationId: Warehouse);

        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Przesunięcie wymaga różnych lokalizacji")]
    public void StockTransfer_RequiresDifferentLocations()
    {
        var act = () => new Document("MM/2026/0001", DocumentType.StockTransfer,
            User, sourceLocationId: Warehouse, targetLocationId: Warehouse);

        act.Should().Throw<DomainException>()
            .WithMessage("*muszą być różne*");
    }

    [Fact(DisplayName = "Rozchód wewnętrzny nie dopuszcza lokalizacji docelowej")]
    public void InternalIssue_RejectsTargetLocation()
    {
        var act = () => new Document("RW/2026/0001", DocumentType.InternalIssue,
            User, sourceLocationId: Van, targetLocationId: Warehouse);

        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Nowy dokument otrzymuje status roboczy")]
    public void NewDocument_HasDraftStatus()
    {
        var document = CreateTransfer();

        document.Status.Should().Be(DocumentStatus.Draft);
        document.IsEditable.Should().BeTrue();
    }

    [Fact(DisplayName = "Powtórne dodanie tego samego towaru sumuje ilości")]
    public void AddLine_MergesDuplicateProduct()
    {
        var document = CreateTransfer();
        var product = Guid.NewGuid();
        
        document.AddLine(product, 10m);
        document.AddLine(product, 5m);

        document.Lines.Should().HaveCount(1);
        document.Lines.Single().Quantity.Should().Be(15m);

    }

    [Fact(DisplayName = "Dokument bez pozycji nie może zostać zatwierdzony")]
    public void MarkAsPosted_RejectsDocumentWithoutLines()
    {
        var document = CreateTransfer();

        var act = () => document.MarkAsPosted();

        act.Should().Throw<DomainException>()
            .WithMessage("*bez pozycji*");
    }

    [Fact(DisplayName = "Zatwierdzony dokument nie podlega edycji")]
    public void PostedDocument_IsNoLongerEditable()
    {
        var document = CreateTransfer();
        document.AddLine(Guid.NewGuid(), 10m);
        document.MarkAsPosted();

        document.IsEditable.Should().BeFalse();
        document.PostedAtUtc.Should().NotBeNull();

        var act = () => document.AddLine(Guid.NewGuid(), 1m);
        act.Should().Throw<DomainException>();
    }

    [Fact(DisplayName = "Dokumentu nie można zatwierdzić dwukrotnie")]
    public void MarkAsPosted_RejectsSecondCall()
    {
        var document = CreateTransfer();
        document.AddLine(Guid.NewGuid(), 10m);
        document.MarkAsPosted();

        var act = () => document.MarkAsPosted();

        act.Should().Throw<DomainException>();
    }

    private static Document CreateTransfer()
        => new("MM/2026/0001", DocumentType.StockTransfer, User, Warehouse, Van);
}