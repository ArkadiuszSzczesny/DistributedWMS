using System.Security.Claims;
using DistributedWMS.Application.Common.Interfaces;
using DistributedWMS.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace DistributedWMS.Infrastructure.Authentication;

/// <summary>
/// Odczytuje tożsamość użytkownika z oświadczeń zawartych w tokenie dostępowym.
/// </summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public UserRole? Role
    {
        get
        {
            var value = Principal?.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRole>(value, out var role) ? role : null;
        }
    }

    public Guid? AssignedLocationId
    {
        get
        {
            var value = Principal?.FindFirst("assignedLocation")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}