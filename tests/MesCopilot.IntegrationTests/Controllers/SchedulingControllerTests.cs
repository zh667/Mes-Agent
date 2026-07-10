using System.Net;
using System.Net.Http.Json;
using MesCopilot.Api.Dtos.Scheduling;
using MesCopilot.Application.Dtos.Scheduling;
using MesCopilot.Domain.Entities.Products;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using EquipmentEntity = MesCopilot.Domain.Entities.Equipment.Equipment;

namespace MesCopilot.IntegrationTests.Controllers;

public class SchedulingControllerTests : IClassFixture<WorkOrdersApiFactory>
{
    private readonly WorkOrdersApiFactory _factory;

    public SchedulingControllerTests(WorkOrdersApiFactory factory) { _factory = factory; }

    [Fact]
    public async Task Generate_CreatesSequentialOperations()
    {
        int workOrderId = await SeedSchedulableOrderAsync();
        using HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/scheduling/generate", new GenerateScheduleRequest
        {
            WorkOrderIds = [workOrderId],
            ScheduleStartUtc = DateTime.UtcNow
        });

        response.EnsureSuccessStatusCode();
        ScheduleGenerationResultDto? result = await response.Content.ReadFromJsonAsync<ScheduleGenerationResultDto>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Operations.Count);
    }

    [Fact]
    public async Task Generate_MoreThanFiftyOrdersReturnsBadRequest()
    {
        using HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/scheduling/generate", new GenerateScheduleRequest
        {
            WorkOrderIds = Enumerable.Range(1, 51).ToArray(),
            ScheduleStartUtc = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<int> SeedSchedulableOrderAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        CurrentTenantContext tenant = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
        tenant.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: false));
        MesDbContext context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
        string suffix = Guid.NewGuid().ToString("N")[..8];
        ProductionLine line = new() { Code = $"SL-{suffix}", Name = "Schedule Line" };
        Workstation station = new() { Code = $"SW-{suffix}", Name = "Schedule Station", ProductionLine = line };
        Product product = new() { Code = $"SP-{suffix}", Name = "Schedule Product", Unit = "pcs" };
        ProcessRoute route = new() { Code = $"SR-{suffix}", Name = "Route", Product = product, IsActive = true };
        route.ProcessSteps.Add(new ProcessStep { Code = $"S1-{suffix}", Name = "Cut", Workstation = station, Sequence = 1, StandardTime = 1m });
        route.ProcessSteps.Add(new ProcessStep { Code = $"S2-{suffix}", Name = "Inspect", Workstation = station, Sequence = 2, StandardTime = 1m });
        EquipmentEntity equipment = new() { Code = $"SE-{suffix}", Name = "Machine", ProductionLine = line, Workstation = station, IsActive = true };
        WorkOrder order = new() { Code = $"WO-{suffix}", Product = product, ProductionLine = line, PlannedQuantity = 2, PlannedStartTime = DateTime.UtcNow, PlannedEndTime = DateTime.UtcNow.AddDays(1), Status = WorkOrderStatus.NotScheduled };
        context.AddRange(route, equipment, order);
        await context.SaveChangesAsync();
        return order.Id;
    }
}
