using System.Collections.Concurrent;
using MesCopilot.Api.Hubs;
using MesCopilot.Application.Dtos.Devices;
using MesCopilot.Application.Dtos.Realtime;
using MesCopilot.Application.Services.Devices;
using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Caching;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Devices;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.Api.HostedServices;

public sealed class DeviceCollectionCoordinator : IAsyncDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDeviceConnectorFactory _connectorFactory;
    private readonly IDeviceStatusCache _cache;
    private readonly IHubContext<EquipmentHub> _hub;
    private readonly ILogger<DeviceCollectionCoordinator> _logger;
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _running = new();

    public DeviceCollectionCoordinator(
        IServiceScopeFactory scopeFactory,
        IDeviceConnectorFactory connectorFactory,
        IDeviceStatusCache cache,
        IHubContext<EquipmentHub> hub,
        ILogger<DeviceCollectionCoordinator> logger)
    {
        _scopeFactory = scopeFactory;
        _connectorFactory = connectorFactory;
        _cache = cache;
        _hub = hub;
        _logger = logger;
    }

    public Task StartAsync(int connectionId, string tenantId, CancellationToken applicationStopping = default)
    {
        _running.GetOrAdd(connectionId, id =>
        {
            CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(applicationStopping);
            _ = ObserveRunAsync(id, tenantId, source.Token);
            return source;
        });
        return Task.CompletedTask;
    }

    public async Task StopAsync(int connectionId)
    {
        if (_running.TryRemove(connectionId, out CancellationTokenSource? source))
        {
            await source.CancelAsync();
            source.Dispose();
        }
    }

    public bool IsRunning(int connectionId) => _running.ContainsKey(connectionId);

    private async Task RunAsync(int connectionId, string tenantId, CancellationToken cancellationToken)
    {
        int attempt = 0;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                DeviceConnectionRuntime? runtime = await LoadRuntimeAsync(connectionId, tenantId, cancellationToken);
                if (runtime is null) return;
                await using IDeviceConnector connector = _connectorFactory.Create(runtime.Protocol, runtime.Settings, runtime.EquipmentId);
                try
                {
                    await connector.ConnectAsync(cancellationToken);
                    await RecordResultAsync(runtime, true, null, cancellationToken);
                    attempt = 0;
                    await foreach (DeviceReading reading in connector.SubscribeAsync(cancellationToken))
                    {
                        try
                        {
                            await ProcessReadingAsync(runtime, reading, cancellationToken);
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(
                                exception,
                                "Persisting reading for device connection {ConnectionId} failed; the protocol connection remains active.",
                                connectionId);
                        }
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    attempt++;
                    string errorCode = exception.GetType().Name;
                    _logger.LogWarning(exception, "Device connection {ConnectionId} failed on attempt {Attempt}.", connectionId, attempt);
                    await RecordResultAsync(runtime, false, errorCode, CancellationToken.None);
                    int delaySeconds = attempt switch { 1 => 1, 2 => 2, 3 => 4, 4 => 8, _ => 30 };
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                }
                finally
                {
                    try
                    {
                        await connector.DisconnectAsync(CancellationToken.None);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(exception, "Device connection {ConnectionId} cleanup failed.", connectionId);
                    }
                }
            }
        }
        finally
        {
            if (_running.TryRemove(connectionId, out CancellationTokenSource? source)) source.Dispose();
        }
    }

    private async Task ObserveRunAsync(int connectionId, string tenantId, CancellationToken cancellationToken)
    {
        try
        {
            await RunAsync(connectionId, tenantId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when a connection or the host is stopped.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Device collection task {ConnectionId} stopped unexpectedly.", connectionId);
        }
    }

    private async Task<DeviceConnectionRuntime?> LoadRuntimeAsync(int id, string tenantId, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        CurrentTenantContext tenant = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
        tenant.Initialize(new TenantResolution(tenantId, IsPlatformAdmin: true));
        IDeviceConnectionService service = scope.ServiceProvider.GetRequiredService<IDeviceConnectionService>();
        return await service.GetRuntimeAsync(id, cancellationToken);
    }

    private async Task RecordResultAsync(DeviceConnectionRuntime runtime, bool connected, string? errorCode, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        CurrentTenantContext tenant = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
        tenant.Initialize(new TenantResolution(runtime.TenantId, IsPlatformAdmin: true));
        await scope.ServiceProvider.GetRequiredService<IDeviceConnectionService>()
            .RecordConnectionResultAsync(runtime.Id, connected, errorCode, cancellationToken);
    }

    private async Task ProcessReadingAsync(DeviceConnectionRuntime runtime, DeviceReading reading, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(reading.State, true, out EquipmentState state)) return;
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        CurrentTenantContext tenant = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
        tenant.Initialize(new TenantResolution(runtime.TenantId, IsPlatformAdmin: true));
        MesDbContext context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
        var equipment = await context.Equipment.AsNoTracking().FirstOrDefaultAsync(item => item.Id == runtime.EquipmentId, cancellationToken);
        if (equipment is null) return;
        context.EquipmentStatuses.Add(new EquipmentStatus
        {
            EquipmentId = runtime.EquipmentId,
            State = state,
            StartTime = reading.Timestamp
        });
        await context.SaveChangesAsync(cancellationToken);
        EquipmentStatusUpdate update = new(runtime.EquipmentId, equipment.Code, equipment.ProductionLineId, state.ToString(), reading.Timestamp);
        await _cache.SetAsync(runtime.TenantId, runtime.EquipmentId, update, cancellationToken);
        await _hub.Clients.Group(EquipmentHub.EquipmentGroup(runtime.TenantId, runtime.EquipmentId))
            .SendAsync("EquipmentStatusChanged", update, cancellationToken);
        if (equipment.ProductionLineId.HasValue)
        {
            await _hub.Clients.Group(EquipmentHub.ProductionLineGroup(runtime.TenantId, equipment.ProductionLineId.Value))
                .SendAsync("EquipmentStatusChanged", update, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (int id in _running.Keys) await StopAsync(id);
    }
}
