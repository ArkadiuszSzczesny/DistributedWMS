namespace DistributedWMS.Shared.Users;

public record UserDto(
    Guid Id,
    string UserName,
    string FullName,
    string Role,
    Guid? AssignedLocationId,
    string? AssignedLocationCode); 