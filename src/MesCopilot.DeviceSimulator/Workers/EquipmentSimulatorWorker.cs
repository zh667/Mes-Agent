using MesCopilot.DeviceSimulator.Services;
using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MesCopilot.Infrastructure.Tenancy;

namespace MesCopilot.DeviceSimulator.Workers;

public class EquipmentSimulatorWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EquipmentSimulatorWorker> _logger;
    private readonly EquipmentStateCalculator _calculator;
    private readonly Random _random = new();

    public EquipmentSimulatorWorker(
        IServiceProvider serviceProvider,
        ILogger<EquipmentSimulatorWorker> logger,
        EquipmentStateCalculator calculator)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _calculator = calculator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
    }

    private async Task SimulateEquipmentAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        CurrentTenantContext tenant = scope.ServiceProvider.GetRequiredService<CurrentTenantContext>();
        tenant.Initialize(new TenantResolution(SeedData.DefaultTenantId, IsPlatformAdmin: true));
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

}
