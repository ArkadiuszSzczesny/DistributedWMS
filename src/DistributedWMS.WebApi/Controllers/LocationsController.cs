using DistributedWMS.Application.Locations;
using DistributedWMS.Shared.Locations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistributedWMS.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locations;

    public LocationsController(ILocationService locations) => _locations = locations;

    ///<summary>Zwraca lokalizacje magazynowe: magazyn główny oraz magazyny mobilne.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LocationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LocationDto>>> GetAll(
        [FromQuery] bool onlyActive = true, CancellationToken ct = default)
        => Ok(await _locations.GetAllAsync(onlyActive, ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _locations.GetByIdAsync(id, ct));

    ///<summary>Tworzy nową lokalizację. Magazyn mobilny wymaga numeru rejestracyjnego.</summary>
    [Authorize(Roles = "Manager")]
    [HttpPost]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LocationDto>> Create(
        [FromBody] CreateLocationRequest request, CancellationToken ct)
    {
        var created = await _locations.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Roles = "Manager")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _locations.DeactivateAsync(id, ct);
        return NoContent();
    }
}


