using MesCopilot.Domain.Entities.Knowledge;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesCopilot.Infrastructure.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(document => document.Id);

        builder.Property(document => document.Title).IsRequired().HasMaxLength(200);
        builder.Property(document => document.FileName).IsRequired().HasMaxLength(255);
        builder.Property(document => document.FilePath).IsRequired().HasMaxLength(500);
        builder.Property(document => document.MimeType).IsRequired().HasMaxLength(100);
        builder.Property(document => document.Type).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(document => document.VectorizationStatus).IsRequired().HasMaxLength(30);

        builder.HasIndex(document => document.FileName);
    }
}

public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("DocumentVersions");
        builder.HasKey(version => version.Id);

        builder.Property(version => version.FileName).IsRequired().HasMaxLength(255);
        builder.Property(version => version.FilePath).IsRequired().HasMaxLength(500);
        builder.Property(version => version.UploadedById).IsRequired().HasMaxLength(450);
        builder.Property(version => version.ChangeNote).HasMaxLength(500);

        builder.HasIndex(version => new { version.DocumentId, version.VersionNumber }).IsUnique();
        builder.HasIndex(version => new { version.DocumentId, version.IsActive });

        builder.HasOne(version => version.Document)
            .WithMany(document => document.Versions)
            .HasForeignKey(version => version.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("DocumentChunks");
        builder.HasKey(chunk => chunk.Id);

        builder.Property(chunk => chunk.Content).IsRequired();
        builder.Property(chunk => chunk.Vector).HasColumnType("vector(1536)");
        builder.Property(chunk => chunk.SectionTitle).HasMaxLength(200);

        builder.HasIndex(chunk => new { chunk.DocumentId, chunk.Sequence }).IsUnique();

        builder.HasOne(chunk => chunk.Document)
            .WithMany(document => document.Chunks)
            .HasForeignKey(chunk => chunk.DocumentId);
    }
}
