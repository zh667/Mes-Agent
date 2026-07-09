using MesCopilot.Application.Dtos;

namespace MesCopilot.Application.Services;

public interface IWorkOrderService
{
    Task<IEnumerable<WorkOrderDto>> GetAllAsync();

    Task<WorkOrderDto?> GetByIdAsync(int id);

    Task<WorkOrderDto> CreateAsync(CreateWorkOrderRequest request);

    Task<WorkOrderDto> StartAsync(int id);

    Task<WorkOrderDto> ReportAsync(int id, ReportProductionRequest request);

    Task<WorkOrderDto> CompleteAsync(int id);

    Task<IEnumerable<WorkOrderDto>> GetTodayWorkOrdersAsync(int? productionLineId = null);

    Task<IEnumerable<WorkOrderDto>> GetDelayedWorkOrdersAsync(DateTime? startDate = null, DateTime? endDate = null);
}
