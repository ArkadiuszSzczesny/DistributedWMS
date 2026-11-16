namespace DistributedWMS.Domain.Enums;

/// <summary>
/// Typy dokumentów magazynowych zgodne z praktyką gospodarki magazynowej.
/// </summary>
public enum DocumentType
{
    /// <summary>PZ - przyjęcie zewnętrzne, dostawa od dostawcy na magazyn główny.</summary>
    GoodsReceipt = 1,

    /// <summary>MM - przesunięcie międzymagazynowe, np. magazyn główny na samochód serwisowy.</summary>
    StockTransfer = 2,
    /// <summary>RW - rozchód wewnętrzny, zużycie materiału na zleceniu serwisowym.</summary>
    InternalIssue = 3,

    /// <summary>IN - inwentaryzacja, korekta stanu ewidencyjnego do stanu faktycznego.</summary>
    Inventory = 4
}
