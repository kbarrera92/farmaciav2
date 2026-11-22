using System.ComponentModel.DataAnnotations;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.Roles;

public sealed record RoleListItemDto(
    int Id,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive,
    int PermissionCount,
    int UserCount);

public sealed record RoleDetailDto(
    int Id,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive,
    IReadOnlyList<string> Permissions,
    AuditInfo Audit);

public sealed class SaveRoleRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede superar 50 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "La descripción no puede superar 200 caracteres.")]
    public string? Description { get; set; }

    public List<string> Permissions { get; set; } = [];
}

public sealed record PermissionDto(string Code, string Name, string? Description);

public sealed record PermissionGroupDto(string Module, IReadOnlyList<PermissionDto> Permissions);
