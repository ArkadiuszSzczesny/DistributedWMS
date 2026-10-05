using DistributedWMS.Application.Common.Exceptions;
using DistributedWMS.Application.Common.Interfaces;
using DistributedWMS.Domain.Entities;
using DistributedWMS.Domain.Enums;
using DistributedWMS.Domain.Exceptions;
using DistributedWMS.Shared.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace DistributedWMS.Application.Documents;

public class DocumentService : IDocumentService
{
    private readonly IWmsDbContext _db;
    private readonly IDocumentNumberGenerator _numberGenerator;

    public DocumentService(IWmsDbContext db, IDocumentNumberGenerator numberGenerator)
    {
        _db = db;
        _numberGenerator = numberGenerator;
    }

    //------ odczyt ------

    public async Task<IReadOnlyList<DocumentListItemDto>> GetAllAsync(
        string? type = null, string? status = null, CancellationToken ct = default)
    {
        var query = _db.Documents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(type) &&
            Enum.TryParse<DocumentType>(type, true, out var parsedType))
            query = query.Where(d => d.Type == parsedType);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<DocumentStatus>(status, true, out var parsedStatus))
            query = query.Where(d => d.Status == parsedStatus);

        return await query
            .OrderByDescending(d => d.DocumentDate)
            .Select(d => new DocumentListItemDto(
                d.Id,
                d.Number,
                d.Type.ToString(),
                d.Status.ToString(),
                d.DocumentDate,
                d.SourceLocation!.Code,
                d.TargetLocation!.Code,
                d.CreatedByUser.FullName,
                d.Reference,
                d.Lines.Count))
            .ToListAsync(ct);
    }

    public async Task<DocumentDetailsDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var document = await LoadFullAsync(id, tracking: false, ct);
        return MapDetailsAsync(document);
    }

    // ------ modyfikacja dokumentu roboczego ------

    public async Task<DocumentDetailsDto> CreateAsync(CreateDocumentRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<DocumentType>(request.Type, true, out var type))
            throw new DomainException($"Nieznany typ dokumentu: {request.Type}");

        await EnsureUserExistsAsync(request.CreatedByUserId, ct);
        await EnsureLocationExistsAsync(request.SourceLocationId, ct);
        await EnsureLocationExistsAsync(request.TargetLocationId, ct);

        var number = await _numberGenerator.GenerateAsync(type, ct);

        var document = new Document(
            number,
            type,
            request.CreatedByUserId,
            request.SourceLocationId,
            request.TargetLocationId,
            request.Reference);

        document.Notes = request.Notes;

        _db.Documents.Add(document);
        await _db.SaveChangesAsync(ct);

        return MapDetailsAsync(await LoadFullAsync(document.Id, tracking: false, ct));
    }

    public async Task<DocumentDetailsDto> AddLineAsync(
        Guid documentId, AddDocumentLineRequest request, CancellationToken ct = default)
    {
        var document = await LoadFullAsync(documentId, tracking: true, ct);

        var productExists = await _db.Products.AnyAsync(p => p.Id == request.ProductId, ct);
        if (!productExists)
            throw new NotFoundException("Towar", request.ProductId);

        document.AddLine(request.ProductId, request.Quantity, request.UnitPrice);
        await _db.SaveChangesAsync(ct);

        return MapDetailsAsync(await LoadFullAsync(documentId, tracking: false, ct));
    }

    public async Task<DocumentDetailsDto> RemoveLineAsync(
        Guid documentId, Guid lineId, CancellationToken ct = default)
    {
        var document = await LoadFullAsync(documentId, tracking: true, ct);

        document.RemoveLine(lineId);
        await _db.SaveChangesAsync(ct);

        return MapDetailsAsync(await LoadFullAsync(documentId, tracking: false, ct));
    }

    public async Task DeleteDraftAsync(Guid documentId, CancellationToken ct = default)
    {
        var document = await LoadFullAsync(documentId, tracking: true, ct);

        if (document.Status != DocumentStatus.Draft)
            throw new DomainException("Usunąć można wyłącznie dokument o statusie roboczym.");

        _db.Documents.Remove(document);
        await _db.SaveChangesAsync(ct);
    }

    // ------ księgowanie ------

    /// <summary>
    /// Zatwierdza dokument. Aktualizacja stanów magazynowych oraz zmiana statusu
    /// wykonywane są w ramach pojedynczej transakcji bazodanowej, dzięki czemu
    /// operacja jest niepodzielna: zakończy się w całości albo nie wywoła żadnych skutków.
    /// </summary>
    public async Task<DocumentDetailsDto> PostAsync(Guid documentId, CancellationToken ct = default)
    {
        await using var transaction = await _db.BeginTransactionAsync(ct);

        try
        {
            var document = await LoadFullAsync(documentId, tracking: true, ct);

            if (document.Status != DocumentStatus.Draft)
                throw new DomainException(
                    $"Dokument {document.Number} ma status {document.Status} i nie może zostać zatwierdzony ponownie.");

            foreach (var line in document.Lines)
            {
                switch (document.Type)
                {
                    case DocumentType.GoodsReceipt:
                        await IncreaseStockAsync(line.ProductId, document.TargetLocationId!.Value, line.Quantity, ct);
                        break;

                    case DocumentType.StockTransfer:
                        // Kolejność ma znaczenie: najpierw rozchód, aby brak towaru
                        // przerwał operację przed zwiększeniem stanu docelowego.
                        await DecreaseStockAsync(line.ProductId, document.SourceLocationId!.Value, line.Quantity, ct);
                        await IncreaseStockAsync(line.ProductId, document.TargetLocationId!.Value, line.Quantity, ct);
                        break;

                    case DocumentType.InternalIssue:
                        await DecreaseStockAsync(line.ProductId, document.SourceLocationId!.Value, line.Quantity, ct);
                        break;

                    case DocumentType.Inventory:
                        await SetStockAsync(line.ProductId, document.TargetLocationId!.Value, line.Quantity, ct);
                        break;
                }
            }

            document.MarkAsPosted();

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            throw new DomainException(
                "Stan magazynowy został zmieniony przez innego użytkownika w trakcie zatwierdzania." +
                "Odśwież dane i spróbuj ponownie.");
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return MapDetailsAsync(await LoadFullAsync(documentId, tracking: false, ct));
    }

    // ------ operacje na stanach ------

    private async Task IncreaseStockAsync(Guid productId, Guid locationId, decimal quantity, CancellationToken ct)
    {
        var stock = await GetOrCreateStockAsync(productId, locationId, ct);
        stock.Increase(quantity);
    }

    private async Task DecreaseStockAsync(Guid productId, Guid locationId, decimal quantity, CancellationToken ct)
    {
        var stock = await _db.StockItems
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.LocationId == locationId, ct);

        if (stock is null)
        {
            var product = await _db.Products.AsNoTracking().FirstAsync(p => p.Id == productId, ct);
            var location = await _db.Locations.AsNoTracking().FirstAsync(l => l.Id == locationId, ct);

            throw new DomainException(
                $"Towar {product.Sku} nie występuje w lokalizacji {location.Code}. Rozchód jest niemożliwy.");
        }

        stock.Decrease(quantity);
    }

    private async Task SetStockAsync(Guid productId, Guid locationId, decimal quantity, CancellationToken ct)
    {
        var stock = await GetOrCreateStockAsync(productId, locationId, ct);
        stock.SetQuantity(quantity);
    }

    /// <summary>
    /// Zwraca rekord stanu, tworząc go przy pierwszym ruchu danego towaru
    /// w danej lokalizacji. Dzięki temu baza nie przechowue pustych kombinacji.
    /// </summary>
    private async Task<StockItem> GetOrCreateStockAsync(Guid productId, Guid locationId, CancellationToken ct)
    {
        var stock = await _db.StockItems
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.LocationId == locationId, ct);

        if (stock is not null)
            return stock;

        stock = new StockItem(productId, locationId);
        _db.StockItems.Add(stock);

        return stock;
    }

    // ------ metody pomocnicze ------

    private async Task<Document> LoadFullAsync(Guid id, bool tracking, CancellationToken ct)
    {
        var query = _db.Documents
            .Include(d => d.Lines)
            .Include(d => d.SourceLocation)
            .Include(d => d.TargetLocation)
            .Include(d => d.CreatedByUser)
            .AsQueryable();

        if (!tracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException("Dokument", id);
    }

    private async Task EnsureUserExistsAsync(Guid userId, CancellationToken ct)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == userId && u.IsActive, ct))
            throw new NotFoundException("Użytkownik", userId);
    }

    private async Task EnsureLocationExistsAsync(Guid? locationId, CancellationToken ct)
    {
        if (locationId is null)
            return;

        if (!await _db.Locations.AnyAsync(l => l.Id == locationId && l.IsActive, ct))
            throw new NotFoundException("Lokalizacja", locationId);
    }

    private DocumentDetailsDto MapDetailsAsync(Document d)
    {
        var products = _db.Products.AsNoTracking().ToDictionary(p => p.Id);

        var lines = d.Lines
            .Select(l => new DocumentLineDto(
                l.Id,
                l.ProductId,
                products.TryGetValue(l.ProductId, out var p) ? p.Sku : string.Empty,
                p2(l.ProductId, products),
                products.TryGetValue(l.ProductId, out var p3) ? p3.UnitOfMeasure : string.Empty,
                l.Quantity,
                l.UnitPrice))
            .ToList();

        return new DocumentDetailsDto(
            d.Id,
            d.Number,
            d.Type.ToString(),
            d.Status.ToString(),
            d.DocumentDate,
            d.PostedAtUtc,
            d.SourceLocationId,
            d.SourceLocation?.Code,
            d.TargetLocationId,
            d.TargetLocation?.Code,
            d.CreatedByUser?.FullName ?? string.Empty,
            d.Reference,
            d.Notes,
            d.IsEditable,
            lines);

        static string p2(Guid id, Dictionary<Guid, Product> dict)
            => dict.TryGetValue(id, out var prod) ? prod.Name : string.Empty;
    }
}
