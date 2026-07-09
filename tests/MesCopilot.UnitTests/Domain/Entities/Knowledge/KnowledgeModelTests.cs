using MesCopilot.Domain.Entities.Knowledge;
using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.Domain.Entities.Knowledge;

public class KnowledgeModelTests
{
    [Fact]
    public void Document_ShouldDefaultVectorizationStatusAndInitializeChunks()
    {
        var document = new Document
        {
            Title = "SOP",
            FileName = "sop.pdf",
            FilePath = "/docs/sop.pdf",
            Type = DocumentType.Sop,
            FileSize = 1024,
            MimeType = "application/pdf"
        };

        Assert.Equal("SOP", document.Title);
        Assert.Equal("sop.pdf", document.FileName);
        Assert.Equal("/docs/sop.pdf", document.FilePath);
        Assert.Equal(DocumentType.Sop, document.Type);
        Assert.Equal(1024, document.FileSize);
        Assert.Equal("application/pdf", document.MimeType);
        Assert.Equal("pending", document.VectorizationStatus);
        Assert.NotNull(document.Chunks);
        Assert.Empty(document.Chunks);
    }

    [Fact]
    public void DocumentChunk_ShouldKeepRagMetadata()
    {
        var chunk = new DocumentChunk
        {
            DocumentId = 1,
            Sequence = 2,
            Content = "A102 alarm handling steps",
            Vector = "[0.1,0.2]",
            TokenCount = 128,
            PageNumber = 3,
            SectionTitle = "Alarm A102"
        };

        Assert.Equal(1, chunk.DocumentId);
        Assert.Equal(2, chunk.Sequence);
        Assert.Equal("A102 alarm handling steps", chunk.Content);
        Assert.Equal("[0.1,0.2]", chunk.Vector);
        Assert.Equal(128, chunk.TokenCount);
        Assert.Equal(3, chunk.PageNumber);
        Assert.Equal("Alarm A102", chunk.SectionTitle);
    }
}
