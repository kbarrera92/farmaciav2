using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Branches;
using Pharmacy.Core.Categories;
using Pharmacy.Core.PaymentMethods;
using Pharmacy.Core.Products;
using Pharmacy.Core.Purchases;
using Pharmacy.Core.Roles;
using Pharmacy.Core.Settings;
using Pharmacy.Core.Stocks;
using Pharmacy.Core.Suppliers;
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
    DbSet<Supplier> Suppliers { get; }
    DbSet<Product> Products { get; }
    DbSet<Purchase> Purchases { get; }
    DbSet<PurchaseItem> PurchaseItems { get; }
    DbSet<AccountPayable> AccountsPayable { get; }
    DbSet<Stock> Stocks { get; }

    /// <summary>Used by services that need an explicit transaction across several saves.</summary>
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
