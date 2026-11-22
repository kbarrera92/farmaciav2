using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Branches;
using Pharmacy.Core.Roles;
using Pharmacy.Core.Users;

namespace Pharmacy.Infrastructure.Persistence.Configurations;

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");
        builder.Property(b => b.Name).HasMaxLength(100).IsRequired()
            .UseCollation(DbCollations.SpanishCaseAndAccentInsensitive);
        builder.Property(b => b.Address).HasMaxLength(150).IsRequired();
        builder.Property(b => b.Phone).HasMaxLength(50);
        builder.HasIndex(b => b.Name).IsUnique();
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Code);
        builder.Property(p => p.Code).HasMaxLength(100).IsUnicode(false);
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Module).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(250);

        // The catalog in code is the source of truth; migrations keep the table in sync.
        builder.HasData(Permissions.All.Select(p => new Permission
        {
            Code = p.Code,
            Name = p.Name,
            Module = p.Module,
            Description = p.Description,
        }));
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.Property(r => r.Name).HasMaxLength(50).IsRequired()
            .UseCollation(DbCollations.SpanishCaseAndAccentInsensitive);
        builder.Property(r => r.Description).HasMaxLength(200);
        builder.HasIndex(r => r.Name).IsUnique();

        builder.HasMany(r => r.Permissions)
            .WithOne(rp => rp.Role)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionCode });
        builder.Property(rp => rp.PermissionCode).HasMaxLength(100).IsUnicode(false);

        builder.HasOne(rp => rp.Permission)
            .WithMany()
            .HasForeignKey(rp => rp.PermissionCode)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.Property(u => u.FullName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Username).HasMaxLength(50).IsRequired();
        builder.Property(u => u.NormalizedUsername).HasMaxLength(50).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsUnicode(false).IsRequired();
        builder.HasIndex(u => u.NormalizedUsername).IsUnique();

        builder.HasOne(u => u.Role)
            .WithMany()
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Branch)
            .WithMany()
            .HasForeignKey(u => u.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
