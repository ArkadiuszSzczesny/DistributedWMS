using DistributedWMS.Application.Documents;
using DistributedWMS.Domain.Entities;
using DistributedWMS.Shared.Documents;
using Microsoft.AspNetCore.Mvc;

namespace DistributedWMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documents;

    public DocumentsController(IDocumentService documents) => _documents = documents;

    /// <summary>Zwraca listę dokumentów z opcjonalnym filtrowaniem po typie i statusie.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentListItemDto>>> GetAll(
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
        => Ok(await _documents.GetAllAsync(type, status, ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentListItemDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _documents.GetByIdAsync(id, ct));

    /// <summary>Tworzy dokument roboczy. Numer nadwany jest automatycznie.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentDetailsDto>> Create(
        [FromBody] CreateDocumentRequest request, CancellationToken ct)
    {
        var created = await _documents.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Dodaje pozycję do dokumentu roboczego.</summary>
    [HttpPost("{id:guid}/lines")]
    [ProducesResponseType(typeof(DocumentDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentDetailsDto>> AddLine(
        Guid id, [FromBody] AddDocumentLineRequest request, CancellationToken ct)
    => Ok(await _documents.AddLineAsync(id, request, ct));

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    [ProducesResponseType(typeof(DocumentDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentDetailsDto>> RemoveLine(
        Guid id, Guid lineId, CancellationToken ct)
        => Ok(await _documents.RemoveLineAsync(id, lineId, ct));

    /// <summary>
    /// Zatwierdza dokument i aktualizuje stany magazynowe w ramach jednej transakcji.
    /// Niewystarczający stan w lokalizacji źródłowej powoduje odrzucenie całej operacji.
    /// </summary>
    [HttpPost("{id:guid}/post")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentDetailsDto>> Post(Guid id, CancellationToken ct)
        => Ok(await _documents.PostAsync(id, ct));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteDraft(Guid id, CancellationToken ct)
    {
        await _documents.DeleteDraftAsync(id, ct);
        return NoContent();
    }
}
