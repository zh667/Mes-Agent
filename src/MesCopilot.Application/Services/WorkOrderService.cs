using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.Application.Services;

public class WorkOrderService : IWorkOrderService
{
    private readonly MesDbContext _context;

    public WorkOrderService(MesDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WorkOrderDto>> GetAllAsync()
    {
        var workOrders = await BaseQuery().ToListAsync();
        return workOrders.Select(ToDto);
    }

    public async Task<WorkOrderDto?> GetByIdAsync(int id)
    {
        var workOrder = await BaseQuery().FirstOrDefaultAsync(item => item.Id == id);
        return workOrder is null ? null : ToDto(workOrder);
    }

    public async Task<WorkOrderDto> CreateAsync(CreateWorkOrderRequest request)
    {
        var workOrder = new WorkOrder
        {
            Code = request.Code,
            ProductId = request.ProductId,
            ProductionLineId = request.ProductionLineId,
            PlannedQuantity = request.PlannedQuantity,
            PlannedStartTime = request.PlannedStartTime,
            PlannedEndTime = request.PlannedEndTime,
            Status = WorkOrderStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };

        _context.WorkOrders.Add(workOrder);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(workOrder.Id))!;
    }

    public async Task<WorkOrderDto> StartAsync(int id)
    {
        var workOrder = await FindWorkOrderAsync(id);

        if (workOrder.Status != WorkOrderStatus.Scheduled)
        {
            throw new InvalidOperationException($"Cannot start work order in status {workOrder.Status}.");
        }

        workOrder.Status = WorkOrderStatus.InProgress;
        workOrder.ActualStartTime = DateTime.UtcNow;
        workOrder.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (await GetByIdAsync(id))!;
    }

    public async Task<WorkOrderDto> ReportAsync(int id, ReportProductionRequest request)
    {
        var workOrder = await FindWorkOrderAsync(id);

        if (workOrder.Status != WorkOrderStatus.InProgress)
        {
            throw new InvalidOperationException($"Cannot report for work order in status {workOrder.Status}.");
        }

        workOrder.CompletedQuantity += request.Quantity;
        workOrder.QualifiedQuantity += request.QualifiedQuantity;
        workOrder.UpdatedAt = DateTime.UtcNow;

        var equipmentId = 1;
        _context.ProductionReports.Add(new ProductionReport
        {
            BatchNumber = $"B{DateTime.UtcNow:yyyyMMddHHmmss}-{equipmentId}",
            WorkOrderId = id,
            ProcessStepId = 1,
            EquipmentId = equipmentId,
            OperatorId = request.OperatorId,
            OperatorName = request.OperatorName,
            Quantity = request.Quantity,
            QualifiedQuantity = request.QualifiedQuantity,
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return (await GetByIdAsync(id))!;
    }

    public async Task<WorkOrderDto> CompleteAsync(int id)
    {
        var workOrder = await FindWorkOrderAsync(id);

        if (workOrder.Status != WorkOrderStatus.InProgress)
        {
            throw new InvalidOperationException($"Cannot complete work order in status {workOrder.Status}.");
        }

        workOrder.Status = WorkOrderStatus.Completed;
        workOrder.ActualEndTime = DateTime.UtcNow;
        workOrder.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (await GetByIdAsync(id))!;
    }

    public async Task<IEnumerable<WorkOrderDto>> GetTodayWorkOrdersAsync(int? productionLineId = null)
    {
        var today = DateTime.UtcNow.Date;
        var query = BaseQuery().Where(workOrder => workOrder.CreatedAt.Date == today);

        if (productionLineId.HasValue)
        {
            query = query.Where(workOrder => workOrder.ProductionLineId == productionLineId.Value);
        }

        var workOrders = await query.ToListAsync();
        return workOrders.Select(ToDto);
    }

    public async Task<IEnumerable<WorkOrderDto>> GetDelayedWorkOrdersAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var now = DateTime.UtcNow;
        var query = BaseQuery()
            .Where(workOrder =>
                workOrder.PlannedEndTime < now &&
                workOrder.Status != WorkOrderStatus.Completed &&
                workOrder.Status != WorkOrderStatus.Closed);

        if (startDate.HasValue)
        {
            query = query.Where(workOrder => workOrder.PlannedStartTime >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(workOrder => workOrder.PlannedEndTime <= endDate.Value);
        }

        var workOrders = await query.ToListAsync();
        return workOrders.Select(ToDto);
    }

    private IQueryable<WorkOrder> BaseQuery()
    {
        return _context.WorkOrders
            .Include(workOrder => workOrder.Product)
            .Include(workOrder => workOrder.ProductionLine);
    }

    private async Task<WorkOrder> FindWorkOrderAsync(int id)
    {
        var workOrder = await _context.WorkOrders.FindAsync(id);
        return workOrder ?? throw new InvalidOperationException($"WorkOrder {id} not found.");
    }

    private static WorkOrderDto ToDto(WorkOrder workOrder)
    {
        return new WorkOrderDto(
            workOrder.Id,
            workOrder.Code,
            workOrder.ProductId,
            workOrder.Product?.Name ?? string.Empty,
            workOrder.ProductionLineId,
            workOrder.ProductionLine?.Name ?? string.Empty,
            workOrder.PlannedQuantity,
            workOrder.CompletedQuantity,
            workOrder.QualifiedQuantity,
            workOrder.Status,
            workOrder.PlannedStartTime,
            workOrder.PlannedEndTime,
            workOrder.ActualStartTime,
            workOrder.ActualEndTime,
            workOrder.Progress);
    }
}
