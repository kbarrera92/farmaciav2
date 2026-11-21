namespace Pharmacy.Core.Common;

/// <summary>Entity with creation and update timestamps (UTC), filled automatically on save.</summary>
public abstract class AuditableEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
