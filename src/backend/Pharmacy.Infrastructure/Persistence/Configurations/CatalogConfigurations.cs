using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharmacy.Core.Categories;
using Pharmacy.Core.Products;
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

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.Property(p => p.Name).HasMaxLength(150).IsRequired()
            .UseCollation(DbCollations.SpanishCaseAndAccentInsensitive);
        builder.Property(p => p.Presentation).HasMaxLength(100).IsRequired()
            .UseCollation(DbCollations.SpanishCaseAndAccentInsensitive);
        builder.HasIndex(p => new { p.Name, p.Presentation }).IsUnique();

        builder.Property(p => p.ActiveIngredient).HasMaxLength(150);
        builder.Property(p => p.TherapeuticAction).HasMaxLength(150);
        builder.Property(p => p.Indications).HasMaxLength(150);
        builder.Property(p => p.Contraindications).HasMaxLength(150);
        builder.Property(p => p.Notes).HasMaxLength(250);
        builder.Property(p => p.Measure).HasMaxLength(75);
        builder.Property(p => p.Laboratory).HasMaxLength(100);

        builder.Property(p => p.Barcode).HasMaxLength(25);
        builder.HasIndex(p => p.Barcode).IsUnique().HasFilter("[Barcode] IS NOT NULL");

        builder.Property(p => p.Price).HasPrecision(10, 2);
        builder.Property(p => p.Cost).HasPrecision(10, 2);

        builder.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Supplier).WithMany().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProductBranchSettingConfiguration : IEntityTypeConfiguration<ProductBranchSetting>
{
    public void Configure(EntityTypeBuilder<ProductBranchSetting> builder)
    {
        builder.ToTable("ProductBranchSettings");
        builder.HasKey(s => new { s.ProductId, s.BranchId });
        builder.Property(s => s.Price).HasPrecision(10, 2);
        builder.Property(s => s.Shelf).HasMaxLength(30);

        builder.HasOne(s => s.Product).WithMany(p => p.BranchSettings)
            .HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Branch).WithMany()
            .HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
