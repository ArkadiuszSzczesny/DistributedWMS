using DistributedWMS.Application.Common.Exceptions;
using DistributedWMS.Application.Common.Interfaces;
using DistributedWMS.Domain.Entities;
using DistributedWMS.Domain.Enums;
using DistributedWMS.Domain.Exceptions;
using DistributedWMS.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace DistributedWMS.Application.Users;

public class UserService : IUserService
{
    private const int MinimumPasswordLength = 8;

    private readonly IWmsDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ICurrentUser _currentUser;

    public UserService(IWmsDbContext db, IPasswordHasher hasher, ICurrentUser currentUser)
    {
        _db = db;
        _hasher = hasher;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(
        bool onlyActive = true, CancellationToken ct = default)
    {
        var query = _db.Users.AsNoTracking();

        if (onlyActive)
            query = query.Where(u => u.IsActive);

        return await query
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
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            throw new DomainException($"Nieznana rola użytkownika: {request.Role}.");

        ValidatePassword(request.Password);

        var userName = request.UserName.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.UserName == userName, ct))
            throw new DomainException($"Użytkownik o nazwie {userName} już istnieje.");

        var user = new User(
            request.UserName,
            request.FullName,
            _hasher.Hash(request.Password),
            role);

        if (request.AssignedLocationId is not null)
        {
            var location = await LoadAvailableLocationAsync(request.AssignedLocationId.Value, null, ct);
            user.AssignLocation(location);
        }
        else if (role == UserRole.Technician)
        {
            throw new DomainException("Serwisant wymaga przypisania magazynu mobilnego.");
        }

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return await MapAsync(user.Id, ct);
    }

    public async Task<UserDto> UpdateAsync(
        Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("Użytkownik", id);

        user.SetFullName(request.FullName);

        if (request.AssignedLocationId != user.AssignedLocationId)
        {
            if (request.AssignedLocationId is null)
            {
                if (user.Role == UserRole.Technician)
                    throw new DomainException("Serwisant musi mieć przypisany magazyn mobilny.");

                user.ClearAssignedLocation();
            }
            else
            {
                var location = await LoadAvailableLocationAsync(
                    request.AssignedLocationId.Value, user.Id, ct);

                user.AssignLocation(location);
            }
        }

        user.ModifiedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await MapAsync(user.Id, ct);
    }

    public async Task ChangePasswordAsync(
        Guid id, ChangePasswordRequest request, CancellationToken ct = default)
    {
        ValidatePassword(request.NewPassword);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("Użytkownik", id);

        user.SetPasswordHash(_hasher.Hash(request.NewPassword));
        user.ModifiedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        if (_currentUser.UserId == id)
            throw new DomainException("Nie można dezaktywować własnego konta.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("Użytkownik", id);

        // Ostatni aktywny kierownik nie może zostać wyłączony,
        // ponieważ uniemożliwiłoby to dalsze zarządzanie systemem.
        if (user.Role == UserRole.Manager)
        {
            var activeManagers = await _db.Users
                .CountAsync(u => u.Role == UserRole.Manager && u.IsActive, ct);

            if (activeManagers <= 1)
                throw new DomainException("System musi posiadać co najmniej jednego aktywnego kierownika.");
        }

        user.Deactivate();
        await _db.SaveChangesAsync(ct);
    }

    // ---------- metody pomocnicze ----------

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumPasswordLength)
            throw new DomainException(
                $"Hasło musi zawierać co najmniej {MinimumPasswordLength} znaków.");
    }

    /// <summary>
    /// Pobiera lokalizację i sprawdza, czy nie jest już przypisana innemu użytkownikowi.
    /// Jeden magazyn mobilny może obsługiwać wyłącznie jeden serwisant.
    /// </summary>
    private async Task<Location> LoadAvailableLocationAsync(
        Guid locationId, Guid? currentUserId, CancellationToken ct)
    {
        var location = await _db.Locations.FirstOrDefaultAsync(
            l => l.Id == locationId && l.IsActive, ct)
            ?? throw new NotFoundException("Lokalizacja", locationId);

        var occupant = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.AssignedLocationId == locationId
                                   && u.IsActive
                                   && u.Id != currentUserId, ct);

        if (occupant is not null)
            throw new DomainException(
                $"Magazyn {location.Code} jest już przypisany użytkownikowi {occupant.FullName}.");

        return location;
    }

    private async Task<UserDto> MapAsync(Guid id, CancellationToken ct)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDto(
                u.Id,
                u.UserName,
                u.FullName,
                u.Role.ToString(),
                u.AssignedLocationId,
                u.AssignedLocation != null ? u.AssignedLocation.Code : null))
            .FirstAsync(ct);
    }
}
