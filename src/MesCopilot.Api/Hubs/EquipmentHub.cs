using Microsoft.AspNetCore.SignalR;

namespace MesCopilot.Api.Hubs;

public class EquipmentHub : Hub
{
    private readonly ILogger<EquipmentHub> _logger;

    public EquipmentHub(ILogger<EquipmentHub> logger)
    {
        _logger = logger;
    }

    public async Task SubscribeToEquipment(int equipmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, EquipmentGroup(equipmentId));
    }

    public async Task SubscribeToProductionLine(int lineId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, ProductionLineGroup(lineId));
    }

    public async Task UnsubscribeFromEquipment(int equipmentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, EquipmentGroup(equipmentId));
    }

    public async Task EquipmentStatusChanged(EquipmentStatusUpdate update)
    {
        await Clients.Group(EquipmentGroup(update.EquipmentId)).SendAsync("EquipmentStatusChanged", update);

        if (update.ProductionLineId.HasValue)
        {
            await Clients.Group(ProductionLineGroup(update.ProductionLineId.Value)).SendAsync("EquipmentStatusChanged", update);
        }
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

    private static string EquipmentGroup(int equipmentId)
    {
        return $"equipment-{equipmentId}";
    }

    private static string ProductionLineGroup(int lineId)
    {
        return $"line-{lineId}";
    }
}

public record EquipmentStatusUpdate(
    int EquipmentId,
    string EquipmentCode,
    int? ProductionLineId,
    string State,
    DateTime Timestamp
);
