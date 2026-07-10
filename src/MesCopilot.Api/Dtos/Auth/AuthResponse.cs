namespace MesCopilot.Api.Dtos.Auth;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public UserInfo User { get; set; } = new();
}

public class UserInfo
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
