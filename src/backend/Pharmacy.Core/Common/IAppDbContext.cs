using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Branches;
using Pharmacy.Core.Categories;
using Pharmacy.Core.PaymentMethods;
using Pharmacy.Core.Roles;
using Pharmacy.Core.Settings;
using Pharmacy.Core.Users;

namespace Pharmacy.Core.Common;

/// <summary>
/// Persistence contract used by the application services. Implemented by the EF Core
/// context in Infrastructure; allows services to be tested against an in-memory database.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<Branch> Branches { get; }
    DbSet<CompanySettings> CompanySettings { get; }
    DbSet<PaymentMethod> PaymentMethods { get; }
    DbSet<Category> Categories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
