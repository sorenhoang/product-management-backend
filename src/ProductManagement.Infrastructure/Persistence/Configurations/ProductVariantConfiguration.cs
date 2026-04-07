using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Infrastructure.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id");

        builder.Property(v => v.ProductId)
            .HasColumnName("product_id");

        builder.Property(v => v.Sku)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("sku");

        builder.HasIndex(v => v.Sku).IsUnique();

        builder.Property(v => v.Price)
            .HasPrecision(18, 2)
            .HasColumnName("price");

        builder.Property(v => v.Stock)
            .HasColumnName("stock")
            .HasDefaultValue(0);

        builder.Property(v => v.Attributes)
            .HasColumnName("attributes")
            .HasColumnType("jsonb");

        builder.Property(v => v.RowVersion)
            .IsRowVersion()
            .HasColumnName("row_version");

        builder.HasOne(v => v.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => v.ProductId);
    }
}
