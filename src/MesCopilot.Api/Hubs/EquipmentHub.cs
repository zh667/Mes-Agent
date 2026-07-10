using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.Api.Hubs;

[Authorize(Policy = "RequireOperator")]
public class EquipmentHub : Hub
{
    private readonly ILogger<EquipmentHub> _logger;
    private readonly MesDbContext _context;
    private readonly ITenantContext _tenantContext;

    public EquipmentHub(ILogger<EquipmentHub> logger, MesDbContext context, ITenantContext tenantContext)
    {
        _logger = logger;
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task SubscribeToEquipment(int equipmentId)
    {
        string tenantId = RequireTenantId();
        if (!await _context.Equipment.AsNoTracking().AnyAsync(item => item.Id == equipmentId, Context.ConnectionAborted))
        {
            throw new HubException("Equipment is not available in the active tenant.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, EquipmentGroup(tenantId, equipmentId), Context.ConnectionAborted);
    }

    public async Task SubscribeToProductionLine(int lineId)
    {
        string tenantId = RequireTenantId();
        if (!await _context.ProductionLines.AsNoTracking().AnyAsync(item => item.Id == lineId, Context.ConnectionAborted))
        {
            throw new HubException("Production line is not available in the active tenant.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ProductionLineGroup(tenantId, lineId), Context.ConnectionAborted);
    }

    public async Task UnsubscribeFromEquipment(int equipmentId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            EquipmentGroup(RequireTenantId(), equipmentId),
            Context.ConnectionAborted);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        _logger.LogInformation("SignalR client connected: {ConnectionId}", Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        _logger.LogInformation(exception, "SignalR client disconnected: {ConnectionId}", Context.ConnectionId);
    }

    public static string EquipmentGroup(string tenantId, int equipmentId) =>
        $"tenant-{tenantId}:equipment-{equipmentId}";

    public static string ProductionLineGroup(string tenantId, int lineId) =>
        $"tenant-{tenantId}:line-{lineId}";

    private string RequireTenantId() =>
        string.IsNullOrWhiteSpace(_tenantContext.TenantId)
            ? throw new HubException("An active tenant is required.")
            : _tenantContext.TenantId;
}
