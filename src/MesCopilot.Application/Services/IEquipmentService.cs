using MesCopilot.Application.Dtos;

namespace MesCopilot.Application.Services;

public interface IEquipmentService
{
    Task<IEnumerable<EquipmentDto>> GetAllAsync();

    Task<EquipmentDto?> GetByIdAsync(int id);

    Task<IEnumerable<EquipmentStatusDto>> GetStatusHistoryAsync(int equipmentId);

    Task<IEnumerable<EquipmentAlarmDto>> GetAlarmsAsync(int equipmentId);

    Task<OeeDto> CalculateOeeAsync(int equipmentId, DateTime date);
}
