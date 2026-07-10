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

        builder.Property(conversation => conversation.UserId).IsRequired().HasMaxLength(450);
        builder.Property(conversation => conversation.Mode).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(conversation => conversation.Title).IsRequired().HasMaxLength(200);

        builder.HasIndex(conversation => conversation.UserId);
        builder.HasIndex(conversation => conversation.UpdatedAt);

        builder.HasMany(conversation => conversation.Messages)
            .WithOne(message => message.Conversation)
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        builder.ToTable("ConversationMessages");
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Role).IsRequired().HasConversion<string>().HasMaxLength(32);
        builder.Property(message => message.Content).IsRequired();
        builder.Property(message => message.ToolResults).HasColumnType("jsonb");

        builder.HasIndex(message => new { message.ConversationId, message.CreatedAt });
    }
}
