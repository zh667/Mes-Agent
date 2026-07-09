using MesCopilot.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesCopilot.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Code).IsRequired().HasMaxLength(50);
        builder.Property(product => product.Name).IsRequired().HasMaxLength(200);
        builder.Property(product => product.Specification).HasMaxLength(500);
        builder.Property(product => product.Unit).IsRequired().HasMaxLength(20);

        builder.HasIndex(product => product.Code).IsUnique();

        builder.HasMany(product => product.Boms)
            .WithOne(bom => bom.Product)
            .HasForeignKey(bom => bom.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("Materials");
        builder.HasKey(material => material.Id);

        builder.Property(material => material.Code).IsRequired().HasMaxLength(50);
        builder.Property(material => material.Name).IsRequired().HasMaxLength(200);
        builder.Property(material => material.Specification).HasMaxLength(500);
        builder.Property(material => material.Unit).IsRequired().HasMaxLength(20);
        builder.Property(material => material.StockQuantity).HasPrecision(18, 2);

        builder.HasIndex(material => material.Code).IsUnique();
    }
}

public class BomConfiguration : IEntityTypeConfiguration<Bom>
{
    public void Configure(EntityTypeBuilder<Bom> builder)
    {
        builder.ToTable("Boms");
        builder.HasKey(bom => bom.Id);

        builder.Property(bom => bom.Code).IsRequired().HasMaxLength(50);
        builder.Property(bom => bom.Version).IsRequired().HasMaxLength(20);

        builder.HasIndex(bom => bom.Code).IsUnique();
    }
}

public class BomItemConfiguration : IEntityTypeConfiguration<BomItem>
{
    public void Configure(EntityTypeBuilder<BomItem> builder)
    {
        builder.ToTable("BomItems");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Quantity).HasPrecision(18, 4);
        builder.Property(item => item.Unit).IsRequired().HasMaxLength(20);

        builder.HasOne(item => item.Bom)
            .WithMany(bom => bom.BomItems)
            .HasForeignKey(item => item.BomId);

        builder.HasOne(item => item.Material)
            .WithMany(material => material.BomItems)
            .HasForeignKey(item => item.MaterialId);
    }
}
