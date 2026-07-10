using MesCopilot.Domain.Entities.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesCopilot.Infrastructure.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(conversation => conversation.Id);
        builder.ConfigureTenantEntity();

        builder.Property(conversation => conversation.UserId).IsRequired().HasMaxLength(450);
        builder.Property(conversation => conversation.Mode).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(conversation => conversation.Title).IsRequired().HasMaxLength(200);

        builder.HasIndex(conversation => new { conversation.TenantId, conversation.UserId });
        builder.HasIndex(conversation => new { conversation.TenantId, conversation.UpdatedAt });

        builder.HasMany(conversation => conversation.Messages)
            .WithOne(message => message.Conversation)
            .HasForeignKey(message => new { message.TenantId, message.ConversationId })
            .HasPrincipalKey(conversation => new { conversation.TenantId, conversation.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        builder.ToTable("ConversationMessages", table => table.HasCheckConstraint(
            "CK_ConversationMessages_VerificationState",
            "(\"VerificationJson\" IS NULL AND \"VerificationSchemaVersion\" IS NULL) OR " +
            "(\"VerificationJson\" IS NOT NULL AND \"VerificationSchemaVersion\" = 1)"));
        builder.HasKey(message => message.Id);
        builder.ConfigureTenantEntity();

        builder.Property(message => message.Role).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(message => message.Content).IsRequired();
        builder.Property(message => message.ToolResults).HasColumnType("jsonb");
        builder.Property(message => message.VerificationJson).HasColumnType("jsonb");

        builder.HasIndex(message => new { message.TenantId, message.ConversationId, message.CreatedAt });
    }
}
