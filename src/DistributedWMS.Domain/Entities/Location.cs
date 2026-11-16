using DistributedWMS.Domain.Common;
using DistributedWMS.Domain.Enums;
using DistributedWMS.Domain.Exceptions;

namespace DistributedWMS.Domain.Entities;

/// <summary>
/// Lokalizacja magazynowa w strukturze rozproszonej: magazyn główny
/// lub magazyn mobilny odpowiadający samochodowi serwisowemu.
/// </summary>
public class Location : EntityBase
{
    private Location() { }

    public Location(string code, string name, LocationType type)
    {
        if(string.IsNullOrWhiteSpace(code))
            throw new DomainException("Kod lokalizacji nie może być pusty.");
        if(string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nazwa lokalizacji nie może być pusta.");

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Type = type;
        IsActive = true;
    }

    /// <summary>Kod lokalizacji, np. MG-01 dla magazynu głównego, AUTO-01 dla samochodu</summary>
    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public LocationType Type { get; private set; }

    /// <summary>Numer rejestracyjny - wypełniany wyłącznie dla magazynów mobilnyxh.</summary>
    public string? VehicleRegistration { get; set; }

    public bool IsActive { get; private set; }

    public ICollection<StockItem> StockItems { get; private set; } = new List<StockItem>();

    public bool IsMobile => Type == LocationType.MobileWarehouse;

    public void Deactivate() => IsActive = false;
}

