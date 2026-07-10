using MesCopilot.Application.Services;
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
        services.AddScoped<IMaintenancePredictionService, MaintenancePredictionService>();
        services.AddScoped<IQualityRootCauseService, QualityRootCauseService>();
        return services;
    }
}
