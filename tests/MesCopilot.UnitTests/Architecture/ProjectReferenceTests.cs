using System.Xml.Linq;

namespace MesCopilot.UnitTests.Architecture;

public class ProjectReferenceTests
{
    [Fact]
    public void DeviceSimulator_ShouldNotReferenceApiProject()
    {
        string projectPath = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "src",
                "MesCopilot.DeviceSimulator",
                "MesCopilot.DeviceSimulator.csproj"));
        XDocument project = XDocument.Load(projectPath);

        var projectReferences = project
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(value => value is not null)
            .ToList();

        Assert.DoesNotContain(
            projectReferences,
            reference => reference!.Contains("MesCopilot.Api", StringComparison.OrdinalIgnoreCase));
    }
}
