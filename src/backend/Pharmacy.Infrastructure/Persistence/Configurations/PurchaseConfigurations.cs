using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharmacy.Core.Purchases;
using Pharmacy.Core.Stocks;
using Pharmacy.Core.Users;

namespace Pharmacy.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases");
        builder.Property(p => p.DocumentNumber).HasMaxLength(30).IsRequired()
            .UseCollation(DbCollations.SpanishCaseAndAccentInsensitive);
        builder.Property(p => p.Type).HasConversion<byte>();
        builder.Property(p => p.Total).HasPrecision(10, 2);
        builder.Property(p => p.CancellationReason).HasMaxLength(250);

        // A cancelled purchase releases its invoice number so it can be registered again.
        builder.HasIndex(p => new { p.SupplierId, p.DocumentNumber }).IsUnique().HasFilter("[IsCancelled] = 0");
        builder.HasIndex(p => p.BranchId);
        builder.HasIndex(p => p.PurchaseDate);

        builder.HasOne(p => p.Branch).WithMany().HasForeignKey(p => p.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Supplier).WithMany().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.PaymentMethod).WithMany().HasForeignKey(p => p.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>(p => p.CancelledBy).WithMany().HasForeignKey(p => p.CancelledByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("PurchaseItems");
        builder.Property(i => i.UnitCost).HasPrecision(10, 2);
        builder.Property(i => i.Subtotal).HasPrecision(10, 2);
        builder.HasIndex(i => new { i.PurchaseId, i.ProductId }).IsUnique();
        builder.HasIndex(i => i.ProductId);

        builder.HasOne(i => i.Purchase).WithMany(p => p.Items).HasForeignKey(i => i.PurchaseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AccountPayableConfiguration : IEntityTypeConfiguration<AccountPayable>
{
    public void Configure(EntityTypeBuilder<AccountPayable> builder)
    {
        builder.ToTable("AccountsPayable");
        builder.Property(a => a.Total).HasPrecision(10, 2);
        builder.Property(a => a.Balance).HasPrecision(10, 2);
        builder.Property(a => a.Status).HasConversion<byte>();
        builder.Ignore(a => a.HasPayments);
        builder.HasIndex(a => a.PurchaseId).IsUnique();
        builder.HasIndex(a => a.SupplierId);
        builder.HasIndex(a => a.DueDate);

        builder.HasOne(a => a.Purchase).WithOne(p => p.AccountPayable)
            .HasForeignKey<AccountPayable>(a => a.PurchaseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Supplier).WithMany().HasForeignKey(a => a.SupplierId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.ToTable("Stocks");
        builder.HasKey(s => new { s.BranchId, s.ProductId });
        builder.HasIndex(s => s.ProductId);

        builder.HasOne(s => s.Branch).WithMany().HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Product).WithMany().HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
