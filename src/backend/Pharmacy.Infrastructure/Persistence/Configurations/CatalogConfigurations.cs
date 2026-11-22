using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pharmacy.Core.Categories;

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
