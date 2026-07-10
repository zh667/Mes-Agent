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
        builder.ConfigureTenantEntity();

        builder.Property(product => product.Code).IsRequired().HasMaxLength(50);
        builder.Property(product => product.Name).IsRequired().HasMaxLength(200);
        builder.Property(product => product.Specification).HasMaxLength(500);
        builder.Property(product => product.Unit).IsRequired().HasMaxLength(20);

        builder.HasIndex(product => new { product.TenantId, product.Code }).IsUnique();

        builder.HasMany(product => product.Boms)
            .WithOne(bom => bom.Product)
            .HasForeignKey(bom => new { bom.TenantId, bom.ProductId })
            .HasPrincipalKey(product => new { product.TenantId, product.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("Materials");
        builder.HasKey(material => material.Id);
        builder.ConfigureTenantEntity();

        builder.Property(material => material.Code).IsRequired().HasMaxLength(50);
        builder.Property(material => material.Name).IsRequired().HasMaxLength(200);
        builder.Property(material => material.Specification).HasMaxLength(500);
        builder.Property(material => material.Unit).IsRequired().HasMaxLength(20);
        builder.Property(material => material.StockQuantity).HasPrecision(18, 2);

        builder.HasIndex(material => new { material.TenantId, material.Code }).IsUnique();
    }
}

public class BomConfiguration : IEntityTypeConfiguration<Bom>
{
    public void Configure(EntityTypeBuilder<Bom> builder)
    {
        builder.ToTable("Boms");
        builder.HasKey(bom => bom.Id);
        builder.ConfigureTenantEntity();

        builder.Property(bom => bom.Code).IsRequired().HasMaxLength(50);
        builder.Property(bom => bom.Version).IsRequired().HasMaxLength(20);

        builder.HasIndex(bom => new { bom.TenantId, bom.Code }).IsUnique();
    }
}

public class BomItemConfiguration : IEntityTypeConfiguration<BomItem>
{
    public void Configure(EntityTypeBuilder<BomItem> builder)
    {
        builder.ToTable("BomItems");
        builder.HasKey(item => item.Id);
        builder.ConfigureTenantEntity();

        builder.Property(item => item.Quantity).HasPrecision(18, 4);
        builder.Property(item => item.Unit).IsRequired().HasMaxLength(20);

        builder.HasOne(item => item.Bom)
            .WithMany(bom => bom.BomItems)
            .HasForeignKey(item => new { item.TenantId, item.BomId })
            .HasPrincipalKey(bom => new { bom.TenantId, bom.Id });

        builder.HasOne(item => item.Material)
            .WithMany(material => material.BomItems)
            .HasForeignKey(item => new { item.TenantId, item.MaterialId })
            .HasPrincipalKey(material => new { material.TenantId, material.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.ChildBom)
            .WithMany(bom => bom.ParentBomItems)
            .HasForeignKey(item => new { item.TenantId, item.ChildBomId })
            .HasPrincipalKey(bom => new { bom.TenantId, bom.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_BomItems_MaterialOrChildBom",
            "(\"MaterialId\" IS NOT NULL AND \"ChildBomId\" IS NULL) OR (\"MaterialId\" IS NULL AND \"ChildBomId\" IS NOT NULL)"));
    }
}

public sealed class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.ToTable("InventoryBalances");
        builder.HasKey(balance => balance.Id);
        builder.ConfigureTenantEntity();
        builder.Property(balance => balance.QuantityOnHand).HasPrecision(18, 4);
        builder.Property(balance => balance.QuantityReserved).HasPrecision(18, 4);
        builder.Ignore(balance => balance.AvailableQuantity);
        builder.HasIndex(balance => new { balance.TenantId, balance.MaterialId }).IsUnique();
        builder.HasOne(balance => balance.Material)
            .WithOne(material => material.InventoryBalance)
            .HasForeignKey<InventoryBalance>(balance => new { balance.TenantId, balance.MaterialId })
            .HasPrincipalKey<Material>(material => new { material.TenantId, material.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
