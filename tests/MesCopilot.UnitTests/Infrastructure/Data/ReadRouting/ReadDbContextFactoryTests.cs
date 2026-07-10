using MesCopilot.Infrastructure.Data.ReadRouting;

namespace MesCopilot.UnitTests.Infrastructure.Data.ReadRouting;

public class ReadDbContextFactoryTests
{
    [Fact]
    public void SelectConnectionString_DefaultsToPrimary()
    {
        ReadRoutingOptions options = new()
        {
            Enabled = false,
            PrimaryConnectionString = "Host=primary;Database=mes",
            ReadConnectionString = "Host=replica;Database=mes"
        };

        string result = ReadDbContextFactory.SelectConnectionString(options, requirePrimary: false);

        Assert.Contains("primary", result, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectConnectionString_UsesReplicaOnlyWhenEnabledAndNotSticky()
    {
        ReadRoutingOptions options = new()
        {
            Enabled = true,
            PrimaryConnectionString = "Host=primary;Database=mes",
            ReadConnectionString = "Host=replica;Database=mes"
        };

        Assert.Contains("replica", ReadDbContextFactory.SelectConnectionString(options, requirePrimary: false), StringComparison.Ordinal);
        Assert.Contains("primary", ReadDbContextFactory.SelectConnectionString(options, requirePrimary: true), StringComparison.Ordinal);
    }

    [Fact]
    public void ReadConsistencyContext_RequiresPrimaryDuringStickyWindow()
    {
        ReadConsistencyContext context = new();

        context.RequirePrimary(TimeSpan.FromSeconds(5));

        Assert.True(context.IsPrimaryRequired);
    }
}
