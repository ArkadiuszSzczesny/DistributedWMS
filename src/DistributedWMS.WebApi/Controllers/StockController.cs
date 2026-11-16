using DistributedWMS.Application.Stock;
using DistributedWMS.Shared.Stock;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistributedWMS.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StockController : ControllerBase
{
    private readonly IStockService _stock;

    public StockController(IStockService stock) => _stock = stock;

    /// <summary>Zwraca stany magazynowe we wskazanej lokalizacji.</summary>
    [HttpGet("location/{locationId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<StockItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockItemDto>>> GetByLocation(
        Guid locationId, [FromQuery] bool skipZero = true, CancellationToken ct = default)
        => Ok(await _stock.GetByLocationAsync(locationId, skipZero, ct));

    /// <summary>Zwraca rozmieszczenie wskazanego towaru we wszystkich lokalizacjach.</summary>
    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<StockItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockItemDto>>> GetByProduct(
        Guid productId, CancellationToken ct)
        => Ok(await _stock.GetByProductAsync(productId, ct));

    /// <summary>Raport braków - towary o stanie łącznym poniżej progu minimalnego.</summary>
    [HttpGet("below-minimum")]
    [ProducesResponseType(typeof(IReadOnlyList<StockSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockSummaryDto>>> GetBelowMinimum(CancellationToken ct)
        => Ok(await _stock.GetBelowMinimumAsync(ct));
}