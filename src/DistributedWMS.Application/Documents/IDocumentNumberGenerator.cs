using DistributedWMS.Domain.Enums;

namespace DistributedWMS.Application.Documents;

public interface IDocumentNumberGenerator
{
    Task<string> GenerateAsync(DocumentType type, CancellationToken ct = default);
}