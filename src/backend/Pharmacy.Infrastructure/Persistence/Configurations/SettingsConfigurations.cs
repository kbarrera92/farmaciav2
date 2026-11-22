using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharmacy.Core.PaymentMethods;
using Pharmacy.Core.Settings;

namespace Pharmacy.Infrastructure.Persistence.Configurations;

internal sealed class CompanySettingsConfiguration : IEntityTypeConfiguration<CompanySettings>
{
    public void Configure(EntityTypeBuilder<CompanySettings> builder)
    {
        builder.ToTable("CompanySettings", t =>
            t.HasCheckConstraint("CK_CompanySettings_SingleRow", $"[Id] = {CompanySettings.SingletonId}"));
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.TaxId).HasMaxLength(25);
        builder.Property(c => c.Address).HasMaxLength(150);
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.Email).HasMaxLength(100);
        builder.Property(c => c.LogoContentType).HasMaxLength(50).IsUnicode(false);
    }
}

internal sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("PaymentMethods");
        builder.Property(p => p.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(p => p.Name).IsUnique();
    }
}
