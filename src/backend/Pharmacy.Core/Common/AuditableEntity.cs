using Pharmacy.Core.Users;

namespace Pharmacy.Core.Common;

/// <summary>
/// Entity with audit fields (who and when it was created and last modified). They are filled
/// automatically on save from the current user and clock; services never set them.
/// A null user means the change was made by the system (seed, login bookkeeping).
/// </summary>
public abstract class AuditableEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public int? CreatedByUserId { get; set; }
    public User? CreatedBy { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
    public int? UpdatedByUserId { get; set; }
    public User? UpdatedBy { get; set; }
}

/// <summary>
/// Marks technical properties (login bookkeeping) whose changes are not business modifications:
/// when only these properties change, the "updated" audit fields are left untouched.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotAuditedAttribute : Attribute;
