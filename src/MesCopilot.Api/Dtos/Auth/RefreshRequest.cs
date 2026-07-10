using System.ComponentModel.DataAnnotations;

namespace MesCopilot.Api.Dtos.Auth;

public class RefreshRequest
{
    [Required]
    public string AccessToken { get; set; } = string.Empty;

    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
