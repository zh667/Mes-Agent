using MesCopilot.Application.Services;
using MesCopilot.Application.Services.Verification;
using MesCopilot.Application.Services.Auditing;
using MesCopilot.Application.Services.Bom;
using MesCopilot.Application.Services.Scheduling;
using MesCopilot.Application.Services.Devices;
using Microsoft.Extensions.DependencyInjection;

namespace MesCopilot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddMesCopilotApplication(this IServiceCollection services)
    {
        services.AddScoped<IWorkOrderService, WorkOrderService>();
        services.AddScoped<IEquipmentService, EquipmentService>();
        services.AddScoped<IQualityService, QualityService>();
        services.AddScoped<IKnowledgeService, KnowledgeService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IReportExportService, ReportExportService>();
        services.AddScoped<IVerificationQueryService, VerificationQueryService>();
        services.AddScoped<IMaintenancePredictionService, MaintenancePredictionService>();
        services.AddScoped<IQualityRootCauseService, QualityRootCauseService>();
        services.AddScoped<IAgentAuditService, AgentAuditService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IBomExplosionService, BomExplosionService>();
        services.AddScoped<ISchedulingService, SequentialSchedulingService>();
        services.AddScoped<IDeviceConnectionService, DeviceConnectionService>();
        return services;
    }
}
