using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Branches;
using Pharmacy.Core.Common;
using Pharmacy.Core.PaymentMethods;
using Pharmacy.Core.Roles;
using Pharmacy.Core.Settings;
using Pharmacy.Core.Users;
using Pharmacy.Infrastructure.Persistence;

namespace Pharmacy.UnitTests.TestSupport;

public sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now = Now.Add(by);
}

/// <summary>User executing the operations in the tests; null (system) unless a test sets it.</summary>
public sealed class TestCurrentUser : ICurrentUser
{
    public int? UserId { get; set; }
}

/// <summary>
/// SQLite in-memory database with the real EF model plus the application services wired manually.
/// Seeds: system role "Administrador", role "Vendedor" (users.view), branch "Central" and an admin user.
/// </summary>
public sealed class TestEnvironment : IDisposable
{
    public const string AdminPassword = "Admin12345";

    private readonly SqliteConnection _connection;

    public TestEnvironment()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        CurrentUser = new TestCurrentUser();
        Clock = new MutableTimeProvider(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));
        Db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, Clock, CurrentUser);
        Db.Database.EnsureCreated();

        Hasher = new PasswordHasher<User>();
        PasswordPolicy = new PasswordPolicy(Options.Create(new PasswordPolicyOptions()));
        UserAccess = new UserAccessService(Db, new MemoryCache(new MemoryCacheOptions()));

        Users = new UserService(Db, Hasher, PasswordPolicy, UserAccess, Clock);
        Roles = new RoleService(Db, UserAccess);
        Branches = new BranchService(Db);
        CompanySettings = new CompanySettingsService(Db, Clock);
        PaymentMethods = new PaymentMethodService(Db);
        Auth = new AuthService(Db, Hasher, PasswordPolicy, UserAccess,
            Options.Create(new LoginLockoutOptions { MaxFailedAttempts = 3, LockoutMinutes = 15 }),
            Clock, NullLogger<AuthService>.Instance);

        Seed();
    }

    public MutableTimeProvider Clock { get; }
    public TestCurrentUser CurrentUser { get; }
    public AppDbContext Db { get; }
    public PasswordHasher<User> Hasher { get; }
    public PasswordPolicy PasswordPolicy { get; }
    public UserAccessService UserAccess { get; }
    public UserService Users { get; }
    public RoleService Roles { get; }
    public BranchService Branches { get; }
    public CompanySettingsService CompanySettings { get; }
    public PaymentMethodService PaymentMethods { get; }
    public AuthService Auth { get; }

    public Role AdminRole { get; private set; } = null!;
    public Role SellerRole { get; private set; } = null!;
    public Branch MainBranch { get; private set; } = null!;
    public User Admin { get; private set; } = null!;

    public async Task<UserDetailDto> CreateUserAsync(string username, int? roleId = null, string password = "Clave1234")
    {
        return await Users.CreateAsync(new CreateUserRequest
        {
            FullName = $"Usuario {username}",
            Username = username,
            Password = password,
            RoleId = roleId ?? SellerRole.Id,
            BranchId = MainBranch.Id,
        });
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }

    private void Seed()
    {
        AdminRole = new Role { Name = "Administrador", IsSystem = true };
        SellerRole = new Role
        {
            Name = "Vendedor",
            Permissions = [new RolePermission { PermissionCode = Permissions.UsersView }],
        };
        MainBranch = new Branch { Name = "Central", Address = "Zona 1" };
        Admin = new User
        {
            FullName = "Administrador",
            Username = "admin",
            NormalizedUsername = TextNormalizer.Normalize("admin"),
            Role = AdminRole,
            Branch = MainBranch,
        };
        Admin.PasswordHash = Hasher.HashPassword(Admin, AdminPassword);

        Db.AddRange(AdminRole, SellerRole, MainBranch, Admin);
        Db.SaveChanges();
        Db.ChangeTracker.Clear();
    }
}
