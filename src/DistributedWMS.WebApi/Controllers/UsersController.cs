using DistributedWMS.Application.Common.Interfaces;
using DistributedWMS.Shared.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DistributedWMS.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IWmsDbContext _db;

    public UsersController(IWmsDbContext db) => _db = db;

    /// <summary>Zwraca listę użytkowników wraz z przypisanymi magazynami mobilnymi.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken ct)
    {
        var users = await _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .Select(u => new UserDto(
                u.Id,
                u.UserName,
                u.FullName,
                u.Role.ToString(),
                u.AssignedLocationId,
                u.AssignedLocation != null ? u.AssignedLocation.Code : null))
            .ToListAsync(ct);

        return Ok(users);
    }
}
