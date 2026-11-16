using DistributedWMS.Shared.Documents;

namespace DistributedWMS.Application.Documents;

/// <summary>
/// Obsługa cyklu życia dokumentów magazynowych: tworzenia, modyfikacj
/// pozycji oraz zatwierdzania wpływającego na stany magazynowe.
/// </summary>
public interface IDocumentService
{
    Task<IReadOnlyList<DocumentListItemDto>> GetAllAsync(
        string? type = null, string? status = null, CancellationToken ct = default);

    Task<DocumentDetailsDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<DocumentDetailsDto> CreateAsync(CreateDocumentRequest request, CancellationToken ct = default);

    Task<DocumentDetailsDto> AddLineAsync(Guid documentId, AddDocumentLineRequest request, CancellationToken ct = default);

    Task<DocumentDetailsDto> RemoveLineAsync(Guid documentId, Guid lineId, CancellationToken ct = default);

    /// <summary> Zatwierdza dokument i aktualizuje stany magazynowe w ramach jednej transakcji.</summary>
    Task<DocumentDetailsDto> PostAsync(Guid documentId, CancellationToken ct = default);

    Task DeleteDraftAsync(Guid documentId, CancellationToken ct = default);
}