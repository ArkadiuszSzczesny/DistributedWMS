namespace DistributedWMS.Client.Common;

/// <summary>
/// Odpowiedź błędu w formacie Problem Details (RFC 7807)
/// zwracana przez centralną obsługę wyjątków w warstwie API.
/// </summary>
public class ProblemResponse
{   
    public string? Detail { get; set; }
}
