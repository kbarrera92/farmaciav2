using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharmacy.Core.Common;
using Pharmacy.Core.Users;

namespace Pharmacy.Core.Auth;

public sealed class AuthService(
    IAppDbContext db,
    IPasswordHasher<User> passwordHasher,
    PasswordPolicy passwordPolicy,
    UserAccessService userAccess,
    IOptions<LoginLockoutOptions> lockoutOptions,
    TimeProvider timeProvider,
    ILogger<AuthService> logger)
{
    // Used to spend the same hashing time when the username does not exist (prevents user enumeration by timing).
    private static readonly string DummyHash = new PasswordHasher<User>().HashPassword(new User(), "dummy-password-1");

    private readonly LoginLockoutOptions _lockout = lockoutOptions.Value;

    public async Task<AuthenticatedUser> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var normalized = TextNormalizer.Normalize(username);
        var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == normalized, ct);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (user is null)
        {
            passwordHasher.VerifyHashedPassword(new User(), DummyHash, password);
            logger.LogWarning("Login failed: unknown username {Username}", normalized);
            throw new AuthenticationFailedException();
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Login failed: inactive user {UserId}", user.Id);
            throw new AuthenticationFailedException();
        }

        if (user.LockoutEndUtc is not null && user.LockoutEndUtc > now)
        {
            logger.LogWarning("Login failed: user {UserId} is locked out until {LockoutEnd}", user.Id, user.LockoutEndUtc);
            throw new AuthenticationFailedException();
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            await RegisterFailedAttemptAsync(user, now, ct);
            throw new AuthenticationFailedException();
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = passwordHasher.HashPassword(user, password);

        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        user.LastLoginAtUtc = now;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} logged in", user.Id);
        return ToAuthenticatedUser(user);
    }

    public async Task<AuthenticatedUser> ChangePasswordAsync(
        int userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct)
            ?? throw new NotFoundException("El usuario no existe.");

        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
            throw new ValidationException("currentPassword", "La contraseña actual no es correcta.");

        passwordPolicy.EnsureValid(newPassword, "newPassword");
        if (newPassword == currentPassword)
            throw new ValidationException("newPassword", "La nueva contraseña debe ser distinta de la actual.");

        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
        user.MustChangePassword = false;
        user.RotateSecurityStamp(); // closes sessions opened on other devices
        await db.SaveChangesAsync(ct);
        userAccess.InvalidateUser(userId);

        logger.LogInformation("User {UserId} changed password", userId);
        return ToAuthenticatedUser(user);
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new
            {
                u.Id, u.FullName, u.Username, u.MustChangePassword,
                Role = new NamedReference(u.RoleId, u.Role!.Name),
                Branch = new NamedReference(u.BranchId, u.Branch!.Name),
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("El usuario no existe.");

        var permissions = await userAccess.GetPermissionsAsync(userId, ct);

        return new CurrentUserDto(
            user.Id, user.FullName, user.Username, user.Role, user.Branch, user.MustChangePassword,
            permissions.Order(StringComparer.Ordinal).ToList());
    }

    private async Task RegisterFailedAttemptAsync(User user, DateTime now, CancellationToken ct)
    {
        user.FailedLoginAttempts++;
        if (user.FailedLoginAttempts >= _lockout.MaxFailedAttempts)
        {
            user.LockoutEndUtc = now.AddMinutes(_lockout.LockoutMinutes);
            user.FailedLoginAttempts = 0;
            logger.LogWarning("User {UserId} locked out until {LockoutEnd}", user.Id, user.LockoutEndUtc);
        }
        else
        {
            logger.LogWarning("Login failed: wrong password for user {UserId} (attempt {Attempt})", user.Id, user.FailedLoginAttempts);
        }

        await db.SaveChangesAsync(ct);
    }

    private static AuthenticatedUser ToAuthenticatedUser(User user) =>
        new(user.Id, user.Username, user.SecurityStamp, user.MustChangePassword);
}
