namespace DistributedWMS.Shared.Documents;

/// <summary>Nagłówek dokumentu na liście.</summary>
public record DocumentListItemDto(
    Guid Id,
    string Number,
    string Type,
    string Status,
    DateTime DocumentDate,
    string? SourceLocationCode,
    string? TargetLocationCode,
    string CreatedByUserName,
    string? Reference,
    int LineCount);

public record DocumentLineDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string ProductName,
    string UnitOfMeasure,
    decimal Quantity,
    decimal? UnitPrice);

/// <summary>Pełny dokument wraz z pozycjami.</summary>
public record DocumentDetailsDto(
    Guid Id,
    string Number,
    string Type,
    string Status,
    DateTime DocumentDate,
    DateTime? PostedAtUtc,
    Guid? SourceLocationId,
    string? SourceLocationCode,
    string? TargetLocationCode,
    string CreatedByUserName,
    string? Reference,
    string? Notes,
    bool IsEditable,
    IReadOnlyList<DocumentLineDto> Lines);

public record CreateDocumentRequest(
    string DocumentType,
    Guid? SourceLocationId,
    Guid? TargetLocationId,
    Guid CreatedByUserId,
    string? Reference,
    string? Notes);
    
public record AddDocumentLineRequest(
    Guid ProductId,
    decimal Quantity,
    decimal? UnitPrice);