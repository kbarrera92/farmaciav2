using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Core.Auth;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    [StringLength(128)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [StringLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}

public sealed record NamedReference(int Id, string Name);

public sealed record CurrentUserDto(
    int Id,
    string FullName,
    string Username,
    NamedReference Role,
    NamedReference Branch,
    bool MustChangePassword,
    IReadOnlyList<string> Permissions);

/// <summary>Data the API needs to issue the authentication token.</summary>
public sealed record AuthenticatedUser(int Id, string Username, Guid SecurityStamp, bool MustChangePassword);
