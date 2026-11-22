namespace Pharmacy.Core.Common;

/// <summary>Who and when created and last modified a record (user full names; null = system).</summary>
public sealed record AuditInfo(DateTime CreatedAtUtc, string? CreatedBy, DateTime UpdatedAtUtc, string? UpdatedBy);
