using DistributedWMS.Application.Common.Exceptions;
using DistributedWMS.Application.Common.Interfaces;
using DistributedWMS.Domain.Entities;
using DistributedWMS.Domain.Enums;
using DistributedWMS.Domain.Exceptions;
using DistributedWMS.Shared.Documents;
using Microsoft.EntityFrameworkCore;

namespace DistributedWMS.Application.Documents;

public class DocumentService : IDocumentService
{
    private readonly IWmsDbContext _db;
    private readonly IDocumentNumberGenerator _numberGenerator;
    private readonly ICurrentUser _currentUser;

    public DocumentService(
        IWmsDbContext db,
        IDocumentNumberGenerator numberGenerator,
        ICurrentUser currentUser)
    {
        _db = db;
        _numberGenerator = numberGenerator;
        _currentUser = currentUser;
    }

    // ------ odczyt ------
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
        return await MapDetailsAsync(id, ct);
    }

    // ------ modyfikacja dokumentu roboczego ------
    public async Task<DocumentDetailsDto> CreateAsync(CreateDocumentRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<DocumentType>(request.DocumentType, true, out var type))
            throw new DomainException($"Nieznany typ dokumentu: {request.DocumentType}");

        if (type == DocumentType.Inventory && _currentUser.Role != UserRole.Manager)
            throw new DomainException("Inwentaryzację może przeprowadzić wyłącznie kierownik.");

        if (type == DocumentType.GoodsReceipt && _currentUser.Role == UserRole.Technician)
            throw new DomainException("Przyjęcie zewnętrzne rejestruje magazynier lub kierownik.");

        await EnsureUserExistsAsync(request.CreatedByUserId, ct);
        await EnsureLocationExistsAsync(request.SourceLocationId, ct);
        await EnsureLocationExistsAsync(request.TargetLocationId, ct);
        EnsureSourceLocationAllowed(request.SourceLocationId);

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

        return await MapDetailsAsync(document.Id, ct);
    }

    public async Task<DocumentDetailsDto> AddLineAsync(
        Guid documentId, AddDocumentLineRequest request, CancellationToken ct = default)
    {
        var document = await LoadFullAsync(documentId, tracking: true, ct);

        var productExists = await _db.Products.AnyAsync(p => p.Id == request.ProductId && p.IsActive, ct);
        if (!productExists)
            throw new NotFoundException("Towar", request.ProductId);

        document.AddLine(request.ProductId, request.Quantity, request.UnitPrice);
        await _db.SaveChangesAsync(ct);

        return await MapDetailsAsync(documentId, ct);
    }

    public async Task<DocumentDetailsDto> RemoveLineAsync(
        Guid documentId, Guid lineId, CancellationToken ct = default)
    {
        var document = await LoadFullAsync(documentId, tracking: true, ct);

        document.RemoveLine(lineId);
        await _db.SaveChangesAsync(ct);

        return await MapDetailsAsync(documentId, ct);
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
                "Stan magazynowy został zmieniony przez innego użytkownika w trakcie zatwierdzania. " +
                "Odśwież dane i spróbuj ponownie.");
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return await MapDetailsAsync(documentId, ct);
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
            var missing = await _db.Products.AsNoTracking().FirstAsync(p => p.Id == productId, ct);
            var place = await _db.Locations.AsNoTracking().FirstAsync(l => l.Id == locationId, ct);

            throw new DomainException(
                $"Towar {missing.Sku} nie występuje w lokalizacji {place.Code}. Rozchód jest niemożliwy.");
        }

        if (stock.Quantity < quantity)
        {
            var product = await _db.Products.AsNoTracking().FirstAsync(p => p.Id == productId, ct);

            throw new DomainException(
                $"Niewystarczający stan towaru {product.Sku} ({product.Name}). " +
                $"Dostępne: {stock.Quantity:N3} {product.UnitOfMeasure}, " +
                $"żądane: {quantity:N3} {product.UnitOfMeasure}.");
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
    /// w danej lokalizacji. Dzięki temu baza nie przechowuje pustych kombinacji.
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

    /// <summary>
    /// Weryfikuje uprawnienie do rozchodu z danej lokalizacji. Serwisant może
    /// wystawiać dokumenty wyłącznie z przypisanego mu magazynu mobilnego,
    /// co stanowi autoryzację na poziomie zasobu, uzupełniającą kontrolę rolową.
    /// </summary>
    private void EnsureSourceLocationAllowed(Guid? sourceLocationId)
    {
        if (sourceLocationId is null)
            return;

        if (_currentUser.Role != UserRole.Technician)
            return;

        if (_currentUser.AssignedLocationId != sourceLocationId)
            throw new DomainException(
                "Serwisant może wystawiać dokumenty wyłącznie z przypisanego magazynu mobilnego.");
    }

    /// <summary>
    /// Buduje pełną reprezentację dokumentu. Dane pozycji pobierane są
    /// pojedynczym zapytaniem ze złączeniem kartoteki towarowej.
    /// </summary>
    private async Task<DocumentDetailsDto> MapDetailsAsync(Guid documentId, CancellationToken ct)
    {
        var header = await _db.Documents
            .AsNoTracking()
            .Where(d => d.Id == documentId)
            .Select(d => new
            {
                d.Id,
                d.Number,
                d.Type,
                d.Status,
                d.DocumentDate,
                d.PostedAtUtc,
                d.SourceLocationId,
                SourceCode = d.SourceLocation != null ? d.SourceLocation.Code : null,
                TargetCode = d.TargetLocation != null ? d.TargetLocation.Code : null,
                UserName = d.CreatedByUser.FullName,
                d.Reference,
                d.Notes
            })
            .FirstOrDefaultAsync(ct);

        if (header is null)
            throw new NotFoundException("Dokument", documentId);

        var lines = await _db.DocumentLines
            .AsNoTracking()
            .Where(l => l.DocumentId == documentId)
            .OrderBy(l => l.Product.Sku)
            .Select(l => new DocumentLineDto(
                l.Id,
                l.ProductId,
                l.Product.Sku,
                l.Product.Name,
                l.Product.UnitOfMeasure,
                l.Quantity,
                l.UnitPrice))
            .ToListAsync(ct);

        return new DocumentDetailsDto(
            header.Id,
            header.Number,
            header.Type.ToString(),
            header.Status.ToString(),
            header.DocumentDate,
            header.PostedAtUtc,
            header.SourceLocationId,
            header.SourceCode,
            header.TargetCode,
            header.UserName,
            header.Reference,
            header.Notes,
            header.Status == DocumentStatus.Draft,
            lines);
    }
}
