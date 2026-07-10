using System.Net.Http.Json;
using MesCopilot.Api.Dtos.Devices;
using MesCopilot.Application.Dtos.Devices;
using MesCopilot.Application.Services.Devices;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MesCopilot.IntegrationTests.Controllers;

public class DeviceConnectionsControllerTests : IClassFixture<WorkOrdersApiFactory>
{
    private readonly WorkOrdersApiFactory _factory;
    public DeviceConnectionsControllerTests(WorkOrdersApiFactory factory) { _factory = factory; }

    [Fact]
    public async Task Create_EncryptsConfigurationAndNeverReturnsPassword()
    {
        int equipmentId;
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            CurrentTenantContext tenant = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
            tenant.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: false));
            MesDbContext context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
            equipmentId = await context.Equipment.Select(item => item.Id).FirstAsync();
        }
        using HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/device-connections", new DeviceConnectionRequest
        {
            Name = "MQTT line feed",
            EquipmentId = equipmentId,
            Protocol = DeviceProtocol.Mqtt,
            Host = "mqtt",
            Port = 1883,
            Endpoint = $"mes/equipment/{equipmentId}/state",
            Username = "collector",
            Password = "super-secret",
            UseTls = true,
            RegisterCount = 1
        });

        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("super-secret", json, StringComparison.Ordinal);
        DeviceConnectionDto dto = (await response.Content.ReadFromJsonAsync<DeviceConnectionDto>())!;
        Assert.True(dto.HasCredentials);
        using IServiceScope verifyScope = _factory.Services.CreateScope();
        CurrentTenantContext verifyTenant = verifyScope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
        verifyTenant.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: false));
        MesDbContext verifyContext = verifyScope.ServiceProvider.GetRequiredService<MesDbContext>();
        string encrypted = await verifyContext.DeviceConnections.Where(item => item.Id == dto.Id).Select(item => item.EncryptedConfiguration).SingleAsync();
        Assert.DoesNotContain("super-secret", encrypted, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Update_WithBlankSecret_PreservesStoredCredentials()
    {
        int equipmentId = await GetEquipmentIdAsync();
        using HttpClient client = _factory.CreateClient();
        DeviceConnectionDto created = (await (await client.PostAsJsonAsync("/api/device-connections", new DeviceConnectionRequest
        {
            Name = "MQTT credential retention",
            EquipmentId = equipmentId,
            Protocol = DeviceProtocol.Mqtt,
            Host = "mqtt",
            Port = 1883,
            Endpoint = $"mes/equipment/{equipmentId}/retention",
            Username = "collector",
            Password = "stored-secret",
            UseTls = true,
            RegisterCount = 1
        })).Content.ReadFromJsonAsync<DeviceConnectionDto>())!;

        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/device-connections/{created.Id}", new DeviceConnectionRequest
        {
            Name = created.Name,
            EquipmentId = equipmentId,
            Protocol = DeviceProtocol.Mqtt,
            Host = "mqtt",
            Port = 1883,
            Endpoint = $"mes/equipment/{equipmentId}/retention",
            Username = "",
            Password = "",
            UseTls = true,
            RegisterCount = 1
        });

        response.EnsureSuccessStatusCode();
        using IServiceScope scope = _factory.Services.CreateScope();
        CurrentTenantContext tenant = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
        tenant.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: false));
        DeviceConnectionRuntime runtime = (await scope.ServiceProvider.GetRequiredService<IDeviceConnectionService>()
            .GetRuntimeAsync(created.Id))!;
        Assert.Equal("collector", runtime.Settings.Username);
        Assert.Equal("stored-secret", runtime.Settings.Password);
    }

    [Fact]
    public async Task Create_MqttCredentialsWithoutTls_ReturnsBadRequest()
    {
        int equipmentId = await GetEquipmentIdAsync();
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/device-connections", new DeviceConnectionRequest
        {
            Name = "Unsafe MQTT",
            EquipmentId = equipmentId,
            Protocol = DeviceProtocol.Mqtt,
            Host = "mqtt",
            Port = 1883,
            Endpoint = $"mes/equipment/{equipmentId}/unsafe",
            Username = "collector",
            Password = "plaintext-secret",
            UseTls = false,
            RegisterCount = 1
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<int> GetEquipmentIdAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        CurrentTenantContext tenant = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
        tenant.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: false));
        return await scope.ServiceProvider.GetRequiredService<MesDbContext>().Equipment.Select(item => item.Id).FirstAsync();
    }
}
