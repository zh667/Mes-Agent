using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MesCopilot.Api.Dtos.Auth;
using MesCopilot.IntegrationTests.Controllers;

namespace MesCopilot.IntegrationTests.Auth;

public class AccessTokenRevocationTests
{
    [Fact]
    public async Task Logout_ImmediatelyRejectsCurrentAccessToken()
    {
        using AuthApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        string email = $"revoke-{Guid.NewGuid():N}@example.com";
        HttpResponseMessage registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Test123!",
            DisplayName = "Revocation Test"
        });
        AuthResponse auth = (await registerResponse.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        HttpResponseMessage logoutResponse = await client.PostAsync("/api/auth/logout", null);
        HttpResponseMessage meResponse = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }
}
