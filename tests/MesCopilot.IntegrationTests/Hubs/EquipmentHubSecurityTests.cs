using MesCopilot.Api.Hubs;
using Microsoft.AspNetCore.Authorization;

namespace MesCopilot.IntegrationTests.Hubs;

public sealed class EquipmentHubSecurityTests
{
    [Fact]
    public void Hub_RequiresAuthenticatedOperator()
    {
        AuthorizeAttribute authorize = Assert.Single(
            typeof(EquipmentHub).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal("RequireOperator", authorize.Policy);
    }

    [Fact]
    public void Hub_DoesNotExposeClientCallableBroadcastMethod()
    {
        Assert.DoesNotContain(
            typeof(EquipmentHub).GetMethods(),
            method => method.DeclaringType == typeof(EquipmentHub) && method.Name == "EquipmentStatusChanged");
    }
}
