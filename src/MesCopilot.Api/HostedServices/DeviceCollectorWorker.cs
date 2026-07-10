using MesCopilot.Application.Services.Devices;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.Api.HostedServices;

public sealed class DeviceCollectorWorker : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(5);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DeviceCollectionCoordinator _coordinator;
    private readonly ILogger<DeviceCollectorWorker> _logger;

    public DeviceCollectorWorker(
        IServiceScopeFactory scopeFactory,
        DeviceCollectionCoordinator coordinator,
        ILogger<DeviceCollectorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _coordinator = coordinator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await StartEnabledConnectionsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Device connection discovery failed; retrying after {DelaySeconds} seconds.", ScanInterval.TotalSeconds);
            }

            await Task.Delay(ScanInterval, stoppingToken);
        }
    }

    private async Task StartEnabledConnectionsAsync(CancellationToken stoppingToken)
    {
        List<string> tenantIds;
        await using (AsyncServiceScope rootScope = _scopeFactory.CreateAsyncScope())
        {
            CurrentTenantContext rootTenant = rootScope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
            rootTenant.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: true));
            MesDbContext context = rootScope.ServiceProvider.GetRequiredService<MesDbContext>();
            tenantIds = await context.Tenants.AsNoTracking()
                .Where(tenant => tenant.IsActive)
                .Select(tenant => tenant.Id)
                .ToListAsync(stoppingToken);
        }

        foreach (string tenantId in tenantIds)
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            CurrentTenantContext tenant = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
            tenant.Initialize(new TenantResolution(tenantId, IsPlatformAdmin: true));
            var connections = await scope.ServiceProvider.GetRequiredService<IDeviceConnectionService>()
                .GetEnabledRuntimeAsync(stoppingToken);
            foreach (var connection in connections)
            {
                await _coordinator.StartAsync(connection.Id, connection.TenantId, stoppingToken);
            }
        }
    }
}
