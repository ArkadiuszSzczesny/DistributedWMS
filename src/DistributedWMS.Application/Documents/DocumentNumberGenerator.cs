using DistributedWMS.Application.Common.Interfaces;
using DistributedWMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DistributedWMS.Application.Documents;

/// <summary>
/// Nadaje numery dokumentów w formacie SYMBOL/ROK/KOLEJNY_NUMER, np. MM/2026/0001.
/// Numeracja prowadzona jest odrębnie dla każdego typu dokumentu i każdego roku.
/// </summary>
public class DocumentNumberGenerator : IDocumentNumberGenerator
{
    private readonly IWmsDbContext _db;

    public DocumentNumberGenerator(IWmsDbContext db) => _db = db;

    public async Task<string> GenerateAsync(DocumentType type, CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = GetPrefix(type);
        var pattern = $"{prefix}/{year}/";

        var lastNumber = await _db.Documents
            .AsNoTracking()
            .Where(d => d.Number.StartsWith(pattern))
            .OrderByDescending(d => d.Number)
            .Select(d => d.Number)
            .FirstOrDefaultAsync(ct);

        var next = 1;
        if (lastNumber is not null)
        {
            var suffix = lastNumber.Substring(pattern.Length);
            if (int.TryParse(suffix, out var parsed))
            {
                next = parsed + 1;
            }
        }

        return $"{pattern}{next:D4}";
    }

    /// <summary>Symbole zgodne z polską praktyką obrotu magazynowego.</summary>
    private static string GetPrefix(DocumentType type) => type switch
    {
        DocumentType.GoodsReceipt => "PZ",
        DocumentType.StockTransfer => "MM",
        DocumentType.InternalIssue => "RW",
        DocumentType.Inventory => "IN",
        _ => "DOK"
    };
}