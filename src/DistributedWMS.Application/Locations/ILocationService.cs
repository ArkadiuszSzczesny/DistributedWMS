using DistributedWMS.Shared.Locations;

namespace DistributedWMS.Application.Locations;

public interface ILocationService
{
    Task<IReadOnlyList<LocationDto>> GetAllAsync(bool onlyActive = true, CancellationToken ct = default);
    Task<LocationDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<LocationDto> CreateAsync(CreateLocationRequest request, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, CancellationToken ct = default);
}