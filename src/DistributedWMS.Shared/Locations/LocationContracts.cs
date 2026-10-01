namespace DistributedWMS.Shared.Locations;

public record LocationDto(
    Guid Id,
    string Code,
    string Name,
    string Type,
    string? VehicleRegistration,
    bool IsActive);

public record CreateLocationRequest(
    string Code,
    string Name,
    string Type,
    string? VehicleRegistration);
