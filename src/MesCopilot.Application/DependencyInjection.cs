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
        return services;
    }
}
