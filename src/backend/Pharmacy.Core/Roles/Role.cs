using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.Roles;

public class Role : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// System role (Administrador): implicitly has every permission and cannot be edited,
    /// deactivated or deleted.
    /// </summary>
    public bool IsSystem { get; set; }

    public bool IsActive { get; set; } = true;

    public List<RolePermission> Permissions { get; set; } = [];
}

public class RolePermission
{
    public int RoleId { get; set; }
    public string PermissionCode { get; set; } = string.Empty;

    public Role? Role { get; set; }
    public Permission? Permission { get; set; }
}
