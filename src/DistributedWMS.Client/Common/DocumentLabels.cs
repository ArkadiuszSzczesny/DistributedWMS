namespace DistributedWMS.Client.Common;

/// <summary>
/// Tłumaczy techniczne nazwy typów i statusów dokumentów
/// na oznaczenia stosowane w praktyce magazynowej.
/// </summary>
public static class DocumentLabels
{   
    public static string TypeName(string type) => type switch
    {
        "GoodsReceipt" => "Przyjęcie zewnętrzne",
        "StockTransfer" => "Przesunięcie międzymagazynowe",
        "InternalIssue" => "Rozchód wewnętrzny",
        "Inventory" => "Inwentaryzacja",
        _ => type
    };

    public static string StatusName(string status) => status switch
    {
        "Draft" => "Roboczy",
        "Posted" => "Zatwierdzony",
        _ => status
    };
}
