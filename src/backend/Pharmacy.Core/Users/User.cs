using Pharmacy.Core.Branches;
using Pharmacy.Core.Common;
using Pharmacy.Core.Roles;

namespace Pharmacy.Core.Users;

public class User : AuditableEntity
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    /// <summary>Upper-case username used for case-insensitive uniqueness and lookups.</summary>
    public string NormalizedUsername { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }
    public Role? Role { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Forces the user to set a new password after the next login.</summary>
    public bool MustChangePassword { get; set; }

    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }

    /// <summary>Changes whenever existing sessions must be invalidated (password reset, deactivation).</summary>
    public Guid SecurityStamp { get; set; } = Guid.NewGuid();

    public void RotateSecurityStamp() => SecurityStamp = Guid.NewGuid();
}
