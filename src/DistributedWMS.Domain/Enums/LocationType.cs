namespace DistributedWMS.Domain.Enums;

///<summary>Rodzaj lokalizacji magazynowej w strukturze rozproszonej</summary>
public enum LocationType
{
    /// <summary>Magazyn główny - stacjonarny, przyjmuje dostawy zewnętrzne</summary>
    MainWarehouse = 1,

    /// <summary>Magazyn mobilny - samochód serwisowy przypisany do serwisanta.</summary>
    MobileWarehouse = 2,
}