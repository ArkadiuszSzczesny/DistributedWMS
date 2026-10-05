using DistributedWMS.Shared.Documents;

namespace DistributedWMS.Application.Documents;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentListItemDto>> GetAllAsync(
        string? type = null, string? status = null, CancellationToken ct = default);

    Task<DocumentDetailsDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<DocumentDetailsDto> CreateAsync(CreateDocumentRequest request, CancellationToken ct = default);

    Task<DocumentDetailsDto> AddLineAsync(Guid documentId, AddDocumentLineRequest request, CancellationToken ct = default);
    
    Task<DocumentDetailsDto> RemoveLineAsync(Guid documentId, Guid lineId, CancellationToken ct = default);

    /// <summary>
    /// Zatwierdza dokument i aktualizuje stany magazynowe w ramach jednej transakcji.
    /// </summary>
    Task<DocumentDetailsDto> PostAsync(Guid documentId, CancellationToken ct = default);

    Task DeleteDraftAsync(Guid documentId, CancellationToken ct = default);
}