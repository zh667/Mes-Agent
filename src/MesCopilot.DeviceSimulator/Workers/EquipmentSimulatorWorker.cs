using MesCopilot.Api.Hubs;
using MesCopilot.DeviceSimulator.Services;
using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.DeviceSimulator.Workers;

public class EquipmentSimulatorWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EquipmentSimulatorWorker> _logger;
    private readonly EquipmentStateCalculator _calculator;
    private readonly string _hubUrl;
    private readonly Random _random = new();
    private HubConnection? _hubConnection;

    public EquipmentSimulatorWorker(
        IServiceProvider serviceProvider,
        ILogger<EquipmentSimulatorWorker> logger,
        EquipmentStateCalculator calculator,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _calculator = calculator;
        _hubUrl = configuration["Realtime:HubUrl"] ?? "http://localhost:5000/hubs/equipment";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect()
            .Build();

        await TryStartHubConnectionAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SimulateEquipmentAsync(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in equipment simulation");
            }
        }

        if (_hubConnection is not null)
        {
            await _hubConnection.StopAsync(CancellationToken.None);
            await _hubConnection.DisposeAsync();
        }
    }

    private async Task SimulateEquipmentAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MesDbContext>();

        var inProgressOrders = await context.WorkOrders
            .Where(workOrder => workOrder.Status == WorkOrderStatus.InProgress)
            .ToListAsync(cancellationToken);
        var equipment = await context.Equipment
            .Where(item => item.IsActive)
            .ToListAsync(cancellationToken);
        var processStepId = await context.ProcessSteps
            .Select(step => step.Id)
            .FirstOrDefaultAsync(cancellationToken);

        foreach (var item in equipment)
        {
            var activeWorkOrder = item.ProductionLineId.HasValue
                ? inProgressOrders.FirstOrDefault(workOrder => workOrder.ProductionLineId == item.ProductionLineId.Value)
                : null;
            var state = _calculator.DetermineState(item, activeWorkOrder);
            var timestamp = DateTime.UtcNow;

            context.EquipmentStatuses.Add(new EquipmentStatus
            {
                EquipmentId = item.Id,
                State = state,
                StartTime = timestamp
            });

            if (state == EquipmentState.Running && activeWorkOrder is not null && processStepId > 0)
            {
                AddProductionReport(context, item, activeWorkOrder, processStepId, timestamp);
            }

            if (state == EquipmentState.Alarm)
            {
                context.EquipmentAlarms.Add(new EquipmentAlarm
                {
                    EquipmentId = item.Id,
                    AlarmCode = $"E{_random.Next(1, 100):000}",
                    Message = "Sensor anomaly",
                    Level = 2,
                    OccurredAt = timestamp
                });
            }

            await PublishEquipmentStatusAsync(item, state, timestamp, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Simulated {Count} equipment states", equipment.Count);
    }

    private void AddProductionReport(
        MesDbContext context,
        Equipment equipment,
        WorkOrder workOrder,
        int processStepId,
        DateTime timestamp)
    {
        var output = _calculator.CalculateOutput(equipment, EquipmentState.Running);
        if (output <= 0)
        {
            return;
        }

        var qualifiedQuantity = (int)(output * 0.98);
        context.ProductionReports.Add(new ProductionReport
        {
            BatchNumber = $"B{timestamp:yyyyMMddHHmmss}-{equipment.Id}",
            WorkOrderId = workOrder.Id,
            ProcessStepId = processStepId,
            EquipmentId = equipment.Id,
            OperatorId = "SIM",
            OperatorName = "Simulator",
            Quantity = output,
            QualifiedQuantity = qualifiedQuantity,
            Timestamp = timestamp,
            CreatedAt = timestamp
        });

        workOrder.CompletedQuantity += output;
        workOrder.QualifiedQuantity += qualifiedQuantity;
    }

    private async Task PublishEquipmentStatusAsync(
        Equipment equipment,
        EquipmentState state,
        DateTime timestamp,
        CancellationToken cancellationToken)
    {
        if (_hubConnection is null)
        {
            return;
        }

        if (_hubConnection.State != HubConnectionState.Connected)
        {
            await TryStartHubConnectionAsync(cancellationToken);
        }

        if (_hubConnection.State != HubConnectionState.Connected)
        {
            return;
        }

        var update = new EquipmentStatusUpdate(
            equipment.Id,
            equipment.Code,
            equipment.ProductionLineId,
            state.ToString(),
            timestamp);

        await _hubConnection.SendAsync("EquipmentStatusChanged", update, cancellationToken);
    }

    private async Task TryStartHubConnectionAsync(CancellationToken cancellationToken)
    {
        if (_hubConnection is null || _hubConnection.State == HubConnectionState.Connected)
        {
            return;
        }

        try
        {
            await _hubConnection.StartAsync(cancellationToken);
            _logger.LogInformation("Connected to SignalR hub at {HubUrl}", _hubUrl);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Could not connect to SignalR hub at {HubUrl}", _hubUrl);
        }
    }
}
