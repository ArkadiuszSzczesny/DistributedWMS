namespace DistributedWMS.Shared.Users;

public record UserDto(
    Guid Id,
    string UserName,
    string FullName,
    string Role,
    Guid? AssignedLocationId,
    string? AssignedLocationCode);

public record LoginRequest(string UserName, string Password);

public record LoginResponse(
    string Token,
    Guid UserId,
    string UserName,
    string FullName,
    string Role,
    Guid? AssignedLocationId);