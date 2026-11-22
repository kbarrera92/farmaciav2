using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharmacy.Core.Categories;
using Pharmacy.Core.Suppliers;

namespace Pharmacy.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.Property(c => c.Name).HasMaxLength(50).IsRequired()
            .UseCollation(DbCollations.SpanishCaseAndAccentInsensitive);
        builder.HasIndex(c => c.Name).IsUnique();
    }
}

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired()
            .UseCollation(DbCollations.SpanishCaseAndAccentInsensitive);
        builder.HasIndex(s => s.Name).IsUnique();
        builder.Property(s => s.TaxId).HasMaxLength(50);
        builder.HasIndex(s => s.TaxId).IsUnique().HasFilter("[TaxId] IS NOT NULL");
        builder.Property(s => s.Address).HasMaxLength(150);
        builder.Property(s => s.Phone).HasMaxLength(50);
        builder.Property(s => s.Email).HasMaxLength(75);
        builder.Property(s => s.ContactName).HasMaxLength(100);
        builder.Property(s => s.ContactPhone).HasMaxLength(50);
    }
}
