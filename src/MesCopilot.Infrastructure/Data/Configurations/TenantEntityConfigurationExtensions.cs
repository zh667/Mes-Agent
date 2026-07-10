using MesCopilot.Domain.Common;
using MesCopilot.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesCopilot.Infrastructure.Data.Configurations;

internal static class TenantEntityConfigurationExtensions
{
    public static void ConfigureTenantEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, ITenantEntity
    {
        builder.Property(entity => entity.TenantId)
            .IsRequired()
            .HasMaxLength(36);
        builder.HasAlternateKey(nameof(ITenantEntity.TenantId), "Id");
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(entity => entity.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
