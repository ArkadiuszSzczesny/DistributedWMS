namespace DistributedWMS.Domain.Enums;

///<summary>Cykl życia dokumentu magazynowego.</summary>
public enum DocumentStatus
{
    /// <summary>Dokument w przygotowaniu - nie wpływa na stany magazynowe.</summary>
    Draft = 1,

    /// <summary>Dokument zaksięgowany - stany magazynowe zostały zaktualizowane.</summary>
    Posted = 2,

    /// <summary>Dokument anulowany - skutki zaksięgowania zostały cofnięte.</summary>
    Cancelled = 3
}
