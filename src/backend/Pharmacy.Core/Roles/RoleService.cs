using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.Roles;

public sealed class RoleService(IAppDbContext db, UserAccessService userAccess)
{
    public async Task<IReadOnlyList<RoleListItemDto>> ListAsync(CancellationToken ct = default)
    {
        var allPermissionsCount = Permissions.All.Count;
        return await db.Roles.AsNoTracking()
            .OrderByDescending(r => r.IsSystem)
            .ThenBy(r => r.Name)
            .Select(r => new RoleListItemDto(
                r.Id, r.Name, r.Description, r.IsSystem, r.IsActive,
                r.IsSystem ? allPermissionsCount : r.Permissions.Count,
                db.Users.Count(u => u.RoleId == r.Id)))
            .ToListAsync(ct);
    }

    /// <summary>Active roles as id/name pairs, for the user form.</summary>
    public async Task<IReadOnlyList<NamedReference>> ListActiveLookupAsync(CancellationToken ct = default) =>
        await db.Roles.AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => new NamedReference(r.Id, r.Name))
            .ToListAsync(ct);

    public async Task<RoleDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var role = await db.Roles.AsNoTracking()
            .Include(r => r.Permissions)
            .Include(r => r.CreatedBy)
            .Include(r => r.UpdatedBy)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw NotFound();

        var permissions = role.IsSystem
            ? Permissions.All.Select(p => p.Code).ToList()
            : role.Permissions.Select(p => p.PermissionCode).OrderBy(c => c).ToList();

        return new RoleDetailDto(role.Id, role.Name, role.Description, role.IsSystem, role.IsActive, permissions,
            new AuditInfo(role.CreatedAtUtc, role.CreatedBy?.FullName, role.UpdatedAtUtc, role.UpdatedBy?.FullName));
    }

    public static IReadOnlyList<PermissionGroupDto> GetPermissionCatalog() =>
        Permissions.All
            .GroupBy(p => p.Module)
            .Select(g => new PermissionGroupDto(
                g.Key,
                g.Select(p => new PermissionDto(p.Code, p.Name, p.Description)).ToList()))
            .ToList();

    public async Task<RoleDetailDto> CreateAsync(SaveRoleRequest request, CancellationToken ct = default)
    {
        var name = TextNormalizer.ToSentenceCase(request.Name);
        await EnsureUniqueNameAsync(name, excludeId: null, ct);
        var codes = ValidatePermissionCodes(request.Permissions);

        var role = new Role
        {
            Name = name,
            Description = TextNormalizer.ToSentenceCaseOrNull(request.Description),
            Permissions = codes.Select(c => new RolePermission { PermissionCode = c }).ToList(),
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);

        return await GetAsync(role.Id, ct);
    }

    public async Task<RoleDetailDto> UpdateAsync(int id, SaveRoleRequest request, CancellationToken ct = default)
    {
        var role = await db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw NotFound();
        EnsureNotSystem(role);

        var name = TextNormalizer.ToSentenceCase(request.Name);
        await EnsureUniqueNameAsync(name, excludeId: id, ct);
        var codes = ValidatePermissionCodes(request.Permissions);

        role.Name = name;
        role.Description = TextNormalizer.ToSentenceCaseOrNull(request.Description);
        role.Permissions.RemoveAll(p => !codes.Contains(p.PermissionCode));
        foreach (var code in codes.Where(c => role.Permissions.All(p => p.PermissionCode != c)))
            role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionCode = code });

        await db.SaveChangesAsync(ct);
        userAccess.InvalidateRole(id);

        return await GetAsync(id, ct);
    }

    public async Task<RoleDetailDto> SetStatusAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw NotFound();
        EnsureNotSystem(role);

        if (!isActive && await db.Users.AnyAsync(u => u.RoleId == id && u.IsActive, ct))
            throw new BusinessRuleException(
                "No se puede desactivar un rol asignado a usuarios activos. Reasigne o desactive esos usuarios primero.");

        role.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        userAccess.InvalidateRole(id);

        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw NotFound();
        EnsureNotSystem(role);

        if (await db.Users.AnyAsync(u => u.RoleId == id, ct))
            throw new BusinessRuleException(
                "No se puede eliminar un rol asignado a usuarios. Puede desactivarlo en su lugar.");

        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);
        userAccess.InvalidateRole(id);
    }

    private static HashSet<string> ValidatePermissionCodes(IEnumerable<string>? requested)
    {
        var codes = (requested ?? []).Select(c => c.Trim()).ToHashSet(StringComparer.Ordinal);
        var unknown = codes.Where(c => !Permissions.AllCodes.Contains(c)).ToList();
        if (unknown.Count > 0)
            throw new ValidationException("permissions", $"Permisos no válidos: {string.Join(", ", unknown)}.");
        return codes;
    }

    private static void EnsureNotSystem(Role role)
    {
        if (role.IsSystem)
            throw new BusinessRuleException("El rol de sistema no se puede modificar ni eliminar.");
    }

    /// <summary>The Name column uses a case and accent insensitive collation, so plain equality is enough.</summary>
    private async Task EnsureUniqueNameAsync(string name, int? excludeId, CancellationToken ct)
    {
        var exists = await db.Roles.AnyAsync(
            r => r.Name == name && (excludeId == null || r.Id != excludeId), ct);
        if (exists)
            throw new ConflictException($"Ya existe un rol con el nombre '{name}'.");
    }

    private static NotFoundException NotFound() => new("El rol no existe.");
}
