using MesCopilot.Application.Dtos;

namespace MesCopilot.Application.Services;

public interface IMaintenancePredictionService
{
    Task<MaintenancePredictionDto> PredictMaintenanceAsync(int equipmentId);
}
