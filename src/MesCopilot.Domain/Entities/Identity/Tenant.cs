namespace MesCopilot.Domain.Entities.Identity;

public class Tenant
{
    private string _code = string.Empty;

    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Code
    {
        get => _code;
        set => _code = value.Trim().ToUpperInvariant();
    }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserTenantMembership> Memberships { get; set; } = new List<UserTenantMembership>();
}
