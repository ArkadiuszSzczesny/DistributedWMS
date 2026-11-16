using DistributedWMS.Application.Common.Exceptions;
using DistributedWMS.Application.Common.Interfaces;
using DistributedWMS.Domain.Entities;
using DistributedWMS.Domain.Exceptions;
using DistributedWMS.Domain.Enums;
using DistributedWMS.Shared.Locations;
using Microsoft.EntityFrameworkCore;

namespace DistributedWMS.Application.Locations;

public class LocationService : ILocationService
{
    private readonly IWmsDbContext _db;

    public LocationService(IWmsDbContext db) => _db = db;

    public async Task<IReadOnlyList<LocationDto>> GetAllAsync(bool onlyActive = true, CancellationToken ct = default)
    {
        var query = _db.Locations.AsNoTracking();

        if (onlyActive)
            query = query.Where(l => l.IsActive);

        return await query
            .OrderBy(l => l.Type)
            .ThenBy(l => l.Code)
            .Select(l => new LocationDto(
                l.Id, l.Code, l.Name, l.Type.ToString(), l.VehicleRegistration))
            .ToListAsync(ct);
    }

    public async Task<LocationDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var location = await _db.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new NotFoundException("Lokalizacja", id);

        return Map(location);
    }

    public async Task<LocationDto> CreateAsync(CreateLocationRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<LocationType>(request.Type, ignoreCase: true, out var type))
            throw new DomainException($"Nieprawidłowy typ lokalizacji {request.Type}.");

        var code = request.Code.Trim().ToUpperInvariant();

        if (await _db.Locations.AnyAsync(l => l.Code == code, ct))
            throw new DomainException($"Lokalizacja z kodem {code} już istnieje.");

        var location = new Location(request.Code, request.Name, type);
        
        if (type == LocationType.MobileWarehouse)
        {
            if (string.IsNullOrWhiteSpace(request.VehicleRegistration))
                throw new DomainException("Magazyn mobilny wymaga podania numeru rejestracyjnego pojazdu.");

            location.VehicleRegistration = request.VehicleRegistration.Trim().ToUpperInvariant();
        }

        _db.Locations.Add(location);
        await _db.SaveChangesAsync(ct);

        return Map(location);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var location = await _db.Locations.FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new NotFoundException("Lokalizacja", id);

        var hasStock = await _db.StockItems.AnyAsync(s => s.LocationId == id && s.Quantity > 0, ct);
        if (hasStock)
            throw new DomainException("Nie można wycofać lokalizacji, w której znajduje się towar.");

        var assignedUser = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.AssignedLocationId == id && u.IsActive, ct);

        if (assignedUser is not null)
            throw new DomainException(
                $"Lokalizacja jest przypisana użytkownikowi {assignedUser.FullName}. " +
                "Przypisz go do innego magazynu przed wycofaniem.");

        location.Deactivate();
        await _db.SaveChangesAsync(ct);
    }

    private static LocationDto Map(Location l) => new(
        l.Id, l.Code, l.Name, l.Type.ToString(), l.VehicleRegistration);
}
