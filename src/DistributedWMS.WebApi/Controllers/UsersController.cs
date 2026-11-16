using DistributedWMS.Application.Users;
using DistributedWMS.Shared.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistributedWMS.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    /// <summary>Zwraca listę użytkowników wraz z przypisanymi magazynami mobilnymi.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(
        [FromQuery] bool onlyActive = true, CancellationToken ct = default)
        => Ok(await _users.GetAllAsync(onlyActive, ct));

    /// <summary>Zakłada konto pracownika.</summary>
    [HttpPost]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var created = await _users.CreateAsync(request, ct);
        return Ok(created);
    }

    /// <summary>Aktualizuje dane pracownika oraz przypisanie magazynu mobilnego.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Update(
        Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
        => Ok(await _users.UpdateAsync(id, request, ct));

    /// <summary>Ustawia nowe hasło użytkownika.</summary>
    [HttpPut("{id:guid}/password")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(
        Guid id, [FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await _users.ChangePasswordAsync(id, request, ct);
        return NoContent();
    }

    /// <summary>Wyłącza konto bez usuwania historii wystawionych dokumentów.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _users.DeactivateAsync(id, ct);
        return NoContent();
    }
}
