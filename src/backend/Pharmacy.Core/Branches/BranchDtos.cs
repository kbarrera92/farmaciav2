using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Core.Branches;

public sealed record BranchDto(
    int Id,
    string Name,
    string Address,
    string? Phone,
    bool IsActive,
    int ActiveUserCount);

public sealed class SaveBranchRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(150, ErrorMessage = "La dirección no puede superar 150 caracteres.")]
    public string Address { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "El teléfono no puede superar 50 caracteres.")]
    public string? Phone { get; set; }
}
