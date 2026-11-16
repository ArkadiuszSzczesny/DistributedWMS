using DistributedWMS.Shared.Locations;

namespace DistributedWMS.Application.Locations;

/// <summary>
/// Zarządzanie lokalizacjami magazynowymi: magazynem głównym
/// oraz magazynami mobilnymi przypisanymi do samochodów serwisowych.
/// </summary>
public interface ILocationService
{
    Task<IReadOnlyList<LocationDto>> GetAllAsync(bool onlyActive = true, CancellationToken ct = default);
    Task<LocationDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<LocationDto> CreateAsync(CreateLocationRequest request, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, CancellationToken ct = default);
}