namespace DistributedWMS.Shared.Users;

public record UserDto(
    Guid Id,
    string UserName,
    string FullName,
    string Role,
    Guid? AssignedLocationId,
    string? AssignedLocationCode);

public record LoginRequest(string UserName, string Password);

public record LoginResponse(string Token, string FullName);

public record CreateUserRequest(
    string UserName,
    string FullName,
    string Password,
    string Role,
    Guid? AssignedLocationId);

public record UpdateUserRequest(
    string FullName,
    Guid? AssignedLocationId);

public record ChangePasswordRequest(string NewPassword);