using MesCopilot.Domain.Enums;

namespace MesCopilot.UnitTests.Domain.Enums;

public class DocumentTypeTests
{
    [Theory]
    [InlineData(DocumentType.Sop, 0)]
    [InlineData(DocumentType.MaintenanceManual, 1)]
    [InlineData(DocumentType.ProcessDocument, 2)]
    [InlineData(DocumentType.ExceptionHandling, 3)]
    public void DocumentType_ShouldHaveCorrectValues(DocumentType type, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)type);
    }
}
