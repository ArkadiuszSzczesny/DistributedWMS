namespace DistributedWMS.Domain.Common;

///<summary>
/// Klasa bazowa dla wszystkich encji domenowych.
/// Zapewnia jednolity identyfikator oraz metadane audytowe.
///</summary>
public abstract class EntityBase
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAtUtc { get; set; }
}
