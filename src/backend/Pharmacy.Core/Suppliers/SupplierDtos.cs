using System.ComponentModel.DataAnnotations;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.Suppliers;

public sealed record SupplierDto(
    int Id,
    string Name,
    string? TaxId,
    string? Address,
    string? Phone,
    string? Email,
    string? ContactName,
    string? ContactPhone,
    bool IsActive,
    AuditInfo Audit);

public sealed class SaveSupplierRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "El NIT no puede superar 50 caracteres.")]
    public string? TaxId { get; set; }

    [StringLength(150, ErrorMessage = "La dirección no puede superar 150 caracteres.")]
    public string? Address { get; set; }

    [StringLength(50, ErrorMessage = "El teléfono no puede superar 50 caracteres.")]
    public string? Phone { get; set; }

    [StringLength(75, ErrorMessage = "El correo no puede superar 75 caracteres.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string? Email { get; set; }

    [StringLength(100, ErrorMessage = "El nombre del contacto no puede superar 100 caracteres.")]
    public string? ContactName { get; set; }

    [StringLength(50, ErrorMessage = "El teléfono del contacto no puede superar 50 caracteres.")]
    public string? ContactPhone { get; set; }
}
