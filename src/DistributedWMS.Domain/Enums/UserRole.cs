namespace DistributedWMS.Domain.Enums;

///<summary>Role użytkowników determinujące zakres uprawnień w systemie.</summary>
public enum UserRole
{
    ///<summary>Magazynier - obsługuje przyjęcia i wydania na magazynie głównym.</summary>
    WarehouseOperator = 1,

    ///<summary>Serwisant - rozlicza zużycie z przypisanego magazynu mobilnego.</summary>
    Technician = 2,

    ///<summary>Kierownik - pełen dostęp, raporty, inwentaryzacja, kartoteki.</summary>
    Manager = 3
}