namespace Pharmacy.Core.Auth;

/// <summary>Permission stored in the database. Rows are seeded from <see cref="Permissions"/>.</summary>
public class Permission
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
}
