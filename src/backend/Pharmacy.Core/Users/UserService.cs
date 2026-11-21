using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.Users;

public sealed class UserService(
    IAppDbContext db,
    IPasswordHasher<User> passwordHasher,
    PasswordPolicy passwordPolicy,
    UserAccessService userAccess,
    TimeProvider timeProvider)
{
    public async Task<PagedResult<UserListItemDto>> ListAsync(UserQuery query, CancellationToken ct = default)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var users = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            users = users.Where(u => u.FullName.Contains(search) || u.Username.Contains(search));
        }
        if (query.BranchId is not null)
            users = users.Where(u => u.BranchId == query.BranchId);
        if (query.RoleId is not null)
            users = users.Where(u => u.RoleId == query.RoleId);
        if (query.IsActive is not null)
            users = users.Where(u => u.IsActive == query.IsActive);

        var total = await users.CountAsync(ct);
        var items = await users
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListItemDto(
                u.Id, u.FullName, u.Username,
                new NamedReference(u.RoleId, u.Role!.Name),
                new NamedReference(u.BranchId, u.Branch!.Name),
                u.IsActive,
                u.LockoutEndUtc != null && u.LockoutEndUtc > now,
                u.LastLoginAtUtc))
            .ToListAsync(ct);

        return new PagedResult<UserListItemDto>(items, page, pageSize, total);
    }

    public async Task<UserDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDetailDto(
                u.Id, u.FullName, u.Username,
                new NamedReference(u.RoleId, u.Role!.Name),
                new NamedReference(u.BranchId, u.Branch!.Name),
                u.IsActive, u.MustChangePassword,
                u.LockoutEndUtc != null && u.LockoutEndUtc > now,
                u.LastLoginAtUtc, u.CreatedAtUtc, u.UpdatedAtUtc))
            .FirstOrDefaultAsync(ct)
            ?? throw NotFound();
    }

    public async Task<UserDetailDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var username = request.Username.Trim();
        await EnsureUniqueUsernameAsync(username, excludeId: null, ct);
        await EnsureActiveRoleAsync(request.RoleId!.Value, ct);
        await EnsureActiveBranchAsync(request.BranchId!.Value, ct);
        passwordPolicy.EnsureValid(request.Password);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Username = username,
            NormalizedUsername = TextNormalizer.Normalize(username),
            RoleId = request.RoleId.Value,
            BranchId = request.BranchId.Value,
            MustChangePassword = true,
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return await GetAsync(user.Id, ct);
    }

    public async Task<UserDetailDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct) ?? throw NotFound();
        var username = request.Username.Trim();
        var roleId = request.RoleId!.Value;
        var branchId = request.BranchId!.Value;

        await EnsureUniqueUsernameAsync(username, excludeId: id, ct);
        if (roleId != user.RoleId)
        {
            await EnsureActiveRoleAsync(roleId, ct);
            if (user.IsActive)
                await EnsureNotLastActiveAdministratorAsync(user, "cambiar el rol del", ct);
        }
        if (branchId != user.BranchId)
            await EnsureActiveBranchAsync(branchId, ct);

        user.FullName = request.FullName.Trim();
        user.Username = username;
        user.NormalizedUsername = TextNormalizer.Normalize(username);
        user.RoleId = roleId;
        user.BranchId = branchId;

        await db.SaveChangesAsync(ct);
        userAccess.InvalidateUser(id);

        return await GetAsync(id, ct);
    }

    public async Task<UserDetailDto> SetStatusAsync(int id, bool isActive, int actingUserId, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct) ?? throw NotFound();

        if (!isActive && user.IsActive)
        {
            if (id == actingUserId)
                throw new BusinessRuleException("No puede desactivar su propio usuario.");
            await EnsureNotLastActiveAdministratorAsync(user, "desactivar al", ct);
            user.RotateSecurityStamp(); // closes any open session
        }
        else if (isActive && !user.IsActive)
        {
            await EnsureActiveRoleAsync(user.RoleId, ct);
            await EnsureActiveBranchAsync(user.BranchId, ct);
            user.FailedLoginAttempts = 0;
            user.LockoutEndUtc = null;
        }

        user.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        userAccess.InvalidateUser(id);

        return await GetAsync(id, ct);
    }

    /// <summary>Sets a temporary password, unlocks the account and closes the user's sessions.</summary>
    public async Task ResetPasswordAsync(int id, string newPassword, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct) ?? throw NotFound();
        passwordPolicy.EnsureValid(newPassword, "newPassword");

        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
        user.MustChangePassword = true;
        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        user.RotateSecurityStamp();

        await db.SaveChangesAsync(ct);
        userAccess.InvalidateUser(id);
    }

    private async Task EnsureUniqueUsernameAsync(string username, int? excludeId, CancellationToken ct)
    {
        var normalized = TextNormalizer.Normalize(username);
        var exists = await db.Users.AnyAsync(
            u => u.NormalizedUsername == normalized && (excludeId == null || u.Id != excludeId), ct);
        if (exists)
            throw new ConflictException($"El usuario '{username}' ya existe.");
    }

    private async Task EnsureActiveRoleAsync(int roleId, CancellationToken ct)
    {
        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roleId, ct);
        if (role is null)
            throw new ValidationException("roleId", "El rol no existe.");
        if (!role.IsActive)
            throw new ValidationException("roleId", "El rol está inactivo.");
    }

    private async Task EnsureActiveBranchAsync(int branchId, CancellationToken ct)
    {
        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == branchId, ct);
        if (branch is null)
            throw new ValidationException("branchId", "La sucursal no existe.");
        if (!branch.IsActive)
            throw new ValidationException("branchId", "La sucursal está inactiva.");
    }

    /// <summary>The system must always keep at least one active user with the system role.</summary>
    private async Task EnsureNotLastActiveAdministratorAsync(User user, string action, CancellationToken ct)
    {
        var isAdministrator = await db.Roles.AnyAsync(r => r.Id == user.RoleId && r.IsSystem, ct);
        if (!isAdministrator)
            return;

        var otherActiveAdministrators = await db.Users.AnyAsync(
            u => u.Id != user.Id && u.IsActive && u.Role!.IsSystem, ct);
        if (!otherActiveAdministrators)
            throw new BusinessRuleException($"No se puede {action} último administrador activo.");
    }

    private static NotFoundException NotFound() => new("El usuario no existe.");
}
