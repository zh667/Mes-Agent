using MesCopilot.Agent.Plugins.OeeAgentPlugin;
using MesCopilot.Agent.Plugins.ProductionAgentPlugin;
using MesCopilot.Agent.Plugins.QualityAgentPlugin;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MesCopilot.UnitTests.Agent.Plugins;

public class AgentPluginDependencyTests
{
    [Fact]
    public void ServiceProvider_ResolvesPluginsWithPhase2DTools()
    {
        ServiceCollection services = new();
        services.AddScoped<IWorkOrderService, FakeWorkOrderService>();
        services.AddScoped<IEquipmentService, FakeEquipmentService>();
        services.AddScoped<IQualityService, FakeQualityService>();
        services.AddScoped<IMaintenancePredictionService, FakeMaintenancePredictionService>();
        services.AddScoped<IQualityRootCauseService, FakeQualityRootCauseService>();
        services.AddScoped<ProductionAgentPlugin>();
        services.AddScoped<OeeAgentPlugin>();
        services.AddScoped<QualityAgentPlugin>();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ProductionAgentPlugin>().SuggestScheduleTool);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<OeeAgentPlugin>().PredictMaintenanceTool);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<QualityAgentPlugin>().FiveWhyAnalysisTool);
    }

    private sealed class FakeWorkOrderService : IWorkOrderService
    {
        public Task<IEnumerable<WorkOrderDto>> GetAllAsync() => Task.FromResult<IEnumerable<WorkOrderDto>>([]);

        public Task<WorkOrderDto?> GetByIdAsync(int id) => Task.FromResult<WorkOrderDto?>(null);

        public Task<WorkOrderDto> CreateAsync(CreateWorkOrderRequest request) => throw new NotSupportedException();

        public Task<WorkOrderDto> StartAsync(int id) => throw new NotSupportedException();

        public Task<WorkOrderDto> ReportAsync(int id, ReportProductionRequest request) => throw new NotSupportedException();

        public Task<WorkOrderDto> CompleteAsync(int id) => throw new NotSupportedException();

        public Task<IEnumerable<WorkOrderDto>> GetTodayWorkOrdersAsync(int? productionLineId = null) => Task.FromResult<IEnumerable<WorkOrderDto>>([]);

        public Task<IEnumerable<WorkOrderDto>> GetDelayedWorkOrdersAsync(DateTime? startDate = null, DateTime? endDate = null) => Task.FromResult<IEnumerable<WorkOrderDto>>([]);
    }

    private sealed class FakeEquipmentService : IEquipmentService
    {
        public Task<IEnumerable<EquipmentDto>> GetAllAsync() => Task.FromResult<IEnumerable<EquipmentDto>>([]);

        public Task<EquipmentDto?> GetByIdAsync(int id) => Task.FromResult<EquipmentDto?>(null);

        public Task<IEnumerable<EquipmentStatusDto>> GetStatusHistoryAsync(int equipmentId) => Task.FromResult<IEnumerable<EquipmentStatusDto>>([]);

        public Task<IEnumerable<EquipmentAlarmDto>> GetAlarmsAsync(int equipmentId) => Task.FromResult<IEnumerable<EquipmentAlarmDto>>([]);

        public Task<OeeDto> CalculateOeeAsync(int equipmentId, DateTime date) => throw new NotSupportedException();
    }

    private sealed class FakeQualityService : IQualityService
    {
        public Task<IEnumerable<QualityInspectionDto>> GetInspectionsAsync() => Task.FromResult<IEnumerable<QualityInspectionDto>>([]);

        public Task<QualityInspectionDto> CreateInspectionAsync(CreateQualityInspectionRequest request) => throw new NotSupportedException();

        public Task<BatchTraceDto> TraceBatchAsync(string batchNumber) => throw new NotSupportedException();

        public Task<IEnumerable<DefectAnalysisDto>> AnalyzeDefectsAsync() => Task.FromResult<IEnumerable<DefectAnalysisDto>>([]);
    }

    private sealed class FakeMaintenancePredictionService : IMaintenancePredictionService
    {
        public Task<MaintenancePredictionDto> PredictMaintenanceAsync(int equipmentId) => throw new NotSupportedException();
    }

    private sealed class FakeQualityRootCauseService : IQualityRootCauseService
    {
        public Task<FiveWhyAnalysisDto> AnalyzeAsync(int? defectRecordId, string? symptomDescription) => throw new NotSupportedException();
    }
}
