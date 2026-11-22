using System.ComponentModel.DataAnnotations;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.Settings;

/// <summary>
/// Company data. <see cref="IsConfigured"/> is false until the data is saved for the first time.
/// <see cref="LogoUpdatedAtUtc"/> changes with every new logo and can be used to refresh cached images.
/// </summary>
public sealed record CompanySettingsDto(
    bool IsConfigured,
    string Name,
    string? TaxId,
    string? Address,
    string? Phone,
    string? Email,
    bool HasLogo,
    DateTime? LogoUpdatedAtUtc,
    AuditInfo? Audit);

public sealed class SaveCompanySettingsRequest
{
    [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(25, ErrorMessage = "El NIT no puede superar 25 caracteres.")]
    public string? TaxId { get; set; }

    [StringLength(150, ErrorMessage = "La dirección no puede superar 150 caracteres.")]
    public string? Address { get; set; }

    [StringLength(50, ErrorMessage = "El teléfono no puede superar 50 caracteres.")]
    public string? Phone { get; set; }

    [StringLength(100, ErrorMessage = "El correo no puede superar 100 caracteres.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string? Email { get; set; }
}

public sealed record CompanyLogo(byte[] Content, string ContentType, DateTime UpdatedAtUtc);
