using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Branches;
using Pharmacy.Core.Categories;
using Pharmacy.Core.Common;
using Pharmacy.Core.PaymentMethods;
using Pharmacy.Core.Roles;
using Pharmacy.Core.Settings;
using Pharmacy.Core.Suppliers;
using Pharmacy.Core.Users;

namespace Pharmacy.Infrastructure.Persistence;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    TimeProvider timeProvider,
    ICurrentUser currentUser)
    : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ConfigureAuditRelationships(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// CreatedBy/UpdatedBy reference Users without cascade (users are never deleted, only deactivated).
    /// </summary>
    private static void ConfigureAuditRelationships(ModelBuilder modelBuilder)
    {
        var auditable = modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(AuditableEntity).IsAssignableFrom(t.ClrType))
            .Select(t => t.ClrType)
            .ToList();

        foreach (var type in auditable)
        {
            var entity = modelBuilder.Entity(type);
            entity.HasOne(typeof(User), nameof(AuditableEntity.CreatedBy))
                .WithMany()
                .HasForeignKey(nameof(AuditableEntity.CreatedByUserId))
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(typeof(User), nameof(AuditableEntity.UpdatedBy))
                .WithMany()
                .HasForeignKey(nameof(AuditableEntity.UpdatedByUserId))
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

    private void ApplyAuditFields()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var userId = currentUser.UserId;
        var touchedParents = FindParentsOfChangedChildren();

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>().ToList())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.CreatedByUserId = userId;
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedByUserId = userId;
                continue;
            }

            if (entry.State == EntityState.Modified)
            {
                // Creation data is immutable, whatever the caller did.
                entry.Property(e => e.CreatedAtUtc).IsModified = false;
                entry.Property(e => e.CreatedByUserId).IsModified = false;
            }

            var modified = (entry.State == EntityState.Modified && HasAuditedChanges(entry))
                // e.g. a role whose permission list changed: the role itself was modified.
                || (entry.State is (EntityState.Unchanged or EntityState.Modified) && touchedParents.Contains(entry.Entity));
            if (modified)
            {
                entry.Property(e => e.UpdatedAtUtc).CurrentValue = now;
                entry.Property(e => e.UpdatedByUserId).CurrentValue = userId;
            }
        }
    }

    /// <summary>
    /// Auditable entities whose non-auditable dependents (e.g. RolePermission) were added or deleted.
    /// </summary>
    private HashSet<AuditableEntity> FindParentsOfChangedChildren()
    {
        var parents = new HashSet<AuditableEntity>(ReferenceEqualityComparer.Instance);
        var changedChildren = ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditableEntity && e.State is (EntityState.Added or EntityState.Deleted));

        foreach (var child in changedChildren)
        {
            foreach (var foreignKey in child.Metadata.GetForeignKeys()
                         .Where(fk => typeof(AuditableEntity).IsAssignableFrom(fk.PrincipalEntityType.ClrType)))
            {
                var keyValues = foreignKey.Properties.Select(p => child.Property(p.Name).CurrentValue).ToArray();
                var parent = ChangeTracker.Entries<AuditableEntity>().FirstOrDefault(e =>
                    e.Metadata == foreignKey.PrincipalEntityType
                    && foreignKey.PrincipalKey.Properties
                        .Select(p => e.Property(p.Name).CurrentValue)
                        .SequenceEqual(keyValues));
                if (parent is not null)
                    parents.Add(parent.Entity);
            }
        }

        return parents;
    }

    private static bool HasAuditedChanges(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<AuditableEntity> entry) =>
        entry.Properties.Any(p => p.IsModified
            && p.Metadata.PropertyInfo?.GetCustomAttribute<NotAuditedAttribute>() is null);
}
