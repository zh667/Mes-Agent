using System.Text.RegularExpressions;
using System.Text.Json;

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

    [Fact]
    public void WebDockerfile_ShouldUseNodeVersionCompatibleWithPinnedPnpm()
    {
        string dockerfile = File.ReadAllText(RepositoryPath("web", "Dockerfile"));
        string packageJson = File.ReadAllText(RepositoryPath("web", "package.json"));
        using JsonDocument package = JsonDocument.Parse(packageJson);

        Assert.Contains("FROM node:22-alpine AS deps", dockerfile);
        Assert.Contains("FROM node:22-alpine AS build", dockerfile);
        Assert.Contains("FROM node:22-alpine AS final", dockerfile);
        Assert.Equal("pnpm@11.7.0", package.RootElement.GetProperty("packageManager").GetString());
    }

    [Fact]
    public void WebDockerignore_ShouldExcludeBuildOutputsAndDependencies()
    {
        string dockerignore = File.ReadAllText(RepositoryPath("web", ".dockerignore"));

        foreach (string ignoredPath in new[] { "node_modules", ".next", "coverage", "tsconfig.tsbuildinfo" })
        {
            Assert.Contains(ignoredPath, dockerignore);
        }
    }

    [Fact]
    public void WebDockerfile_ShouldConfigurePnpmFetchResilience()
    {
        string dockerfile = File.ReadAllText(RepositoryPath("web", "Dockerfile"));

        Assert.Contains("pnpm config set fetch-timeout 600000", dockerfile);
        Assert.Contains("pnpm config set fetch-retries 5", dockerfile);
        Assert.Contains("pnpm config set network-concurrency 8", dockerfile);
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
