using System.Text.RegularExpressions;

namespace MesCopilot.UnitTests.Architecture;

public class DeploymentConfigurationTests
{
    [Fact]
    public void WebDockerfile_ShouldCopyNextConfigAndBindToAllInterfaces()
    {
        string dockerfile = File.ReadAllText(RepositoryPath("web", "Dockerfile"));

        Assert.Contains("COPY --from=build /app/next.config.mjs ./", dockerfile);
        Assert.Contains("CMD [\"pnpm\", \"start\", \"-H\", \"0.0.0.0\"]", dockerfile);
        Assert.DoesNotContain("CMD [\"pnpm\", \"start\", \"--\",", dockerfile);
    }

    [Fact]
    public void DockerCompose_ShouldGateSimulatorOnApiHealthcheck()
    {
        string compose = File.ReadAllText(RepositoryPath("docker-compose.yml"));

        Assert.Matches(
            new Regex(
                "api:\\s+build:.*?healthcheck:\\s+test:\\s+\\[\"CMD\", \"curl\", \"-f\", \"http://localhost:8080/health\"\\]",
                RegexOptions.Singleline),
            compose);
        Assert.Matches(
            new Regex(
                "simulator:\\s+build:.*?api:\\s+condition:\\s+service_healthy",
                RegexOptions.Singleline),
            compose);
    }

    [Fact]
    public void ApiDockerfile_ShouldInstallCurlForComposeHealthcheck()
    {
        string dockerfile = File.ReadAllText(RepositoryPath("src", "MesCopilot.Api", "Dockerfile"));

        Assert.Contains("apt-get install -y --no-install-recommends curl", dockerfile);
    }

    private static string RepositoryPath(params string[] segments)
    {
        string[] pathSegments =
        [
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            ..segments
        ];

        return Path.GetFullPath(Path.Combine(pathSegments));
    }
}
