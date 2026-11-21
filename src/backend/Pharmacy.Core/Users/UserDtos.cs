using System.ComponentModel.DataAnnotations;
using Pharmacy.Core.Auth;

namespace Pharmacy.Core.Users;

public sealed record UserListItemDto(
    int Id,
    string FullName,
    string Username,
    NamedReference Role,
    NamedReference Branch,
    bool IsActive,
    bool IsLockedOut,
    DateTime? LastLoginAtUtc);

public sealed record UserDetailDto(
    int Id,
    string FullName,
    string Username,
    NamedReference Role,
    NamedReference Branch,
    bool IsActive,
    bool MustChangePassword,
    bool IsLockedOut,
    DateTime? LastLoginAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed class UserQuery
{
    [StringLength(100)]
    public string? Search { get; set; }

    public int? BranchId { get; set; }
    public int? RoleId { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class UpdateUserRequest
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre completo no puede superar 100 caracteres.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 50 caracteres.")]
    [RegularExpression(@"^[A-Za-z0-9._-]+$",
        ErrorMessage = "El usuario solo puede contener letras, números, punto, guion y guion bajo.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es obligatorio.")]
    public int? RoleId { get; set; }

    [Required(ErrorMessage = "La sucursal es obligatoria.")]
    public int? BranchId { get; set; }
}

public sealed class CreateUserRequest : UpdateUserRequest
{
    /// <summary>Temporary password; the user must change it on first login.</summary>
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;
}

public sealed class ResetPasswordRequest
{
    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    public string NewPassword { get; set; } = string.Empty;
}
