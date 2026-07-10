using MesCopilot.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesCopilot.Infrastructure.Data.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(tenant => tenant.Id);
        builder.Property(tenant => tenant.Id).HasMaxLength(36);
        builder.Property(tenant => tenant.Code).IsRequired().HasMaxLength(50);
        builder.Property(tenant => tenant.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(tenant => tenant.Code).IsUnique();
    }
}

public sealed class UserTenantMembershipConfiguration : IEntityTypeConfiguration<UserTenantMembership>
{
    public void Configure(EntityTypeBuilder<UserTenantMembership> builder)
    {
        builder.ToTable("UserTenantMemberships");
        builder.HasKey(membership => membership.Id);
        builder.Property(membership => membership.UserId).IsRequired().HasMaxLength(450);
        builder.Property(membership => membership.TenantId).IsRequired().HasMaxLength(36);
        builder.Property(membership => membership.Role).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(membership => new { membership.UserId, membership.TenantId }).IsUnique();
        builder.HasOne(membership => membership.User)
            .WithMany(user => user.TenantMemberships)
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(membership => membership.Tenant)
            .WithMany(tenant => tenant.Memberships)
            .HasForeignKey(membership => membership.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
