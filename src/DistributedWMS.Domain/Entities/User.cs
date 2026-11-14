using DistributedWMS.Domain.Common;
using DistributedWMS.Domain.Enums;
using DistributedWMS.Domain.Exceptions;

namespace DistributedWMS.Domain.Entities;

///<summary>
///Użytkownik systemu. Serwisant posiada przypisany magazyn mobilny,
///co stanowi podstawę autoryzacji zasobowej przy rozliczeniu zużycia
///</summary>
public class User : EntityBase
{
    private User() { }

    public User(string userName, string fullName, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new DomainException("Nazwa użytkownika nie może być pusta.");
       
        UserName = userName.Trim().ToLowerInvariant();
        FullName = fullName.Trim();
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
    }

    public string UserName { get; private set; } = null!;

    public string FullName { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public UserRole Role { get; private set; }

    ///<summary>Magazyn mobilny przypisany serwisantowi. Dla pozostałych ról pozostaje pusty.</summary>
    public Guid? AssignedLocationId { get; private set; }

    public Location? AssignedLocation { get; private set; }

    public bool IsActive { get; private set; }

    public void AssignLocation(Location location)
    {
        if (Role != UserRole.Technician)
            throw new DomainException("Magazyn mobilny można przypisać wyłącznie serwisantowi.");
        if(!location.IsMobile)
            throw new DomainException("Serwisantowi można przypisać wyłącznie magazyn mobilny.");

        AssignedLocationId = location.Id;
        AssignedLocation = location;
    }

    public void SetFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Imię i nazwisko nie może być puste.");

        FullName = fullName.Trim();
    }

    public void ClearAssignedLocation()
    {
        AssignedLocationId = null;
        AssignedLocation = null;
    }

    public void SetPasswordHash(string hash) => PasswordHash = hash;

    public void Deactivate() => IsActive = false;
}