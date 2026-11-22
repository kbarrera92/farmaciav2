using Pharmacy.Core.Common;

namespace Pharmacy.Core.Settings;

/// <summary>
/// Company data printed on tickets and reports. Single row (<see cref="SingletonId"/>): the
/// business is one company with several branches.
/// </summary>
public class CompanySettings : AuditableEntity
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public string Name { get; set; } = string.Empty;

    /// <summary>NIT of the company.</summary>
    public string? TaxId { get; set; }

    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public byte[]? Logo { get; set; }
    public string? LogoContentType { get; set; }
    public DateTime? LogoUpdatedAtUtc { get; set; }
}
