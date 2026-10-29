namespace DistributedWMS.Client.Common;

/// <summary>
/// Odpowiedź błędu w formacie Problem Details (RFC 7807)
/// zwracana przez centralną obsługę wyjątków w warswiie API.
/// </summary>
public class ProblemResponse
{
    public string? Title { get; set; }
    public int? Status { get; set; }
    public string? Detail { get; set; }
}
