namespace MesCopilot.Infrastructure.Data.ReadRouting;

public sealed class ReadRoutingOptions
{
    public bool Enabled { get; set; }
    public string PrimaryConnectionString { get; set; } = string.Empty;
    public string? ReadConnectionString { get; set; }
}
