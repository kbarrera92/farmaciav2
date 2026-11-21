using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.Auth;

public sealed record UserAccessState(int UserId, bool IsActive, Guid SecurityStamp, int RoleId);

public sealed record RoleAccess(bool IsActive, bool IsSystem, IReadOnlySet<string> Permissions);

/// <summary>
/// Resolves (and caches) the data needed on every authenticated request: whether the user is still
/// valid and which permissions their role grants. Services invalidate the cache when they change it.
/// </summary>
public sealed class UserAccessService(IAppDbContext db, IMemoryCache cache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly IReadOnlySet<string> NoPermissions = new HashSet<string>();

    public async Task<UserAccessState?> GetUserStateAsync(int userId, CancellationToken ct = default)
    {
        return await cache.GetOrCreateAsync(UserKey(userId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserAccessState(u.Id, u.IsActive, u.SecurityStamp, u.RoleId))
                .FirstOrDefaultAsync(ct);
        });
    }

    public async Task<RoleAccess> GetRoleAccessAsync(int roleId, CancellationToken ct = default)
    {
        var access = await cache.GetOrCreateAsync(RoleKey(roleId), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var role = await db.Roles.AsNoTracking()
                .Where(r => r.Id == roleId)
                .Select(r => new { r.IsActive, r.IsSystem, Codes = r.Permissions.Select(p => p.PermissionCode).ToList() })
                .FirstOrDefaultAsync(ct);

            if (role is null)
                return new RoleAccess(false, false, NoPermissions);

            // An inactive role grants nothing; the system role always grants every permission.
            IReadOnlySet<string> permissions = !role.IsActive
                ? NoPermissions
                : role.IsSystem
                    ? Permissions.AllCodes
                    : role.Codes.ToHashSet(StringComparer.Ordinal);

            return new RoleAccess(role.IsActive, role.IsSystem, permissions);
        });

        return access!;
    }

    public async Task<IReadOnlySet<string>> GetPermissionsAsync(int userId, CancellationToken ct = default)
    {
        var state = await GetUserStateAsync(userId, ct);
        if (state is null || !state.IsActive)
            return NoPermissions;

        return (await GetRoleAccessAsync(state.RoleId, ct)).Permissions;
    }

    public void InvalidateUser(int userId) => cache.Remove(UserKey(userId));

    public void InvalidateRole(int roleId) => cache.Remove(RoleKey(roleId));

    private static string UserKey(int userId) => $"access:user:{userId}";

    private static string RoleKey(int roleId) => $"access:role:{roleId}";
}
