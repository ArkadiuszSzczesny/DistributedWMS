using DistributedWMS.Domain.Enums;

namespace DistributedWMS.Application.Documents;

/// <summary>
/// Nadaje kolejne numery dokumentów magazynowych odrębnie
/// dla każdego typu doumentu i roku obrotowego.
/// </summary>
public interface IDocumentNumberGenerator
{
    Task<string> GenerateAsync(DocumentType type, CancellationToken ct = default);
}