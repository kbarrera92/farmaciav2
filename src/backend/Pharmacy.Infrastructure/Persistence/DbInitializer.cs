using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Branches;
using Pharmacy.Core.Common;
using Pharmacy.Core.Roles;
using Pharmacy.Core.Users;

namespace Pharmacy.Infrastructure.Persistence;

/// <summary>Initial administrator credentials (section "InitialAdmin"), only used when the database has no users.</summary>
public sealed class InitialAdminOptions
{
    public const string SectionName = "InitialAdmin";

    public string Username { get; set; } = "admin";
    public string FullName { get; set; } = "Administrador";
    public string? Password { get; set; }
    public string BranchName { get; set; } = "Sucursal principal";
    public string BranchAddress { get; set; } = "Pendiente de configurar";
}

/// <summary>
/// Creates the system role, a first branch and the first administrator when the database is empty.
/// The administrator must change the password on first login.
/// </summary>
public sealed class DbInitializer(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    PasswordPolicy passwordPolicy,
    IOptions<InitialAdminOptions> options,
    ILogger<DbInitializer> logger)
{
    public const string SystemRoleName = "Administrador";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
            return;

        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Password))
        {
            logger.LogWarning(
                "The database has no users and 'InitialAdmin:Password' is not configured. The initial administrator was not created.");
            return;
        }
        passwordPolicy.EnsureValid(settings.Password, "InitialAdmin:Password");

        var role = await db.Roles.FirstOrDefaultAsync(r => r.IsSystem, ct);
        if (role is null)
        {
            role = new Role { Name = SystemRoleName, Description = "Acceso total al sistema.", IsSystem = true };
            db.Roles.Add(role);
        }

        var branch = await db.Branches.OrderBy(b => b.Id).FirstOrDefaultAsync(ct);
        if (branch is null)
        {
            branch = new Branch
            {
                Name = TextNormalizer.ToSentenceCase(settings.BranchName),
                Address = TextNormalizer.CapitalizeFirst(settings.BranchAddress),
            };
            db.Branches.Add(branch);
        }

        var admin = new User
        {
            FullName = TextNormalizer.ToTitleCase(settings.FullName),
            Username = settings.Username.Trim(),
            NormalizedUsername = TextNormalizer.Normalize(settings.Username),
            Role = role,
            Branch = branch,
            MustChangePassword = true,
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, settings.Password);
        db.Users.Add(admin);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Initial administrator '{Username}' created", admin.Username);
    }
}
