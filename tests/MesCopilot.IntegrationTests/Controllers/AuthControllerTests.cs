using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MesCopilot.Api.Dtos.Auth;
using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace MesCopilot.IntegrationTests.Controllers;

public class AuthControllerTests : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client;
    private readonly AuthApiFactory _factory;

    public AuthControllerTests(AuthApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreatedWithTokensAndUser()
    {
        RegisterRequest request = new()
        {
            Email = $"register-{Guid.NewGuid():N}@example.com",
            Password = "Test123!",
            DisplayName = "Register User",
            Role = "Operator"
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        AuthResponse? auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
        Assert.Equal(request.Email, auth.User.Email);
        Assert.Equal("Operator", auth.User.Role);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        RegisterRequest request = new()
        {
            Email = $"duplicate-{Guid.NewGuid():N}@example.com",
            Password = "Test123!",
            DisplayName = "Duplicate User"
        };

        await _client.PostAsJsonAsync("/api/auth/register", request);
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithSevenCharacterPassword_ReturnsBadRequest()
    {
        RegisterRequest request = new()
        {
            Email = $"weak-password-{Guid.NewGuid():N}@example.com",
            Password = "Aa1!aaa",
            DisplayName = "Weak Password User"
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithTokens()
    {
        string email = $"login-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Test123!",
            DisplayName = "Login User"
        });

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "Test123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AuthResponse? auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "missing@example.com",
            Password = "wrong"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidTokenPair_RotatesTokens()
    {
        AuthResponse auth = await RegisterAsync("Refresh User");

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest
        {
            AccessToken = auth.AccessToken,
            RefreshToken = auth.RefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AuthResponse? refreshed = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(refreshed);
        Assert.NotEqual(auth.RefreshToken, refreshed.RefreshToken);
        Assert.False(string.IsNullOrWhiteSpace(refreshed.AccessToken));
    }

    [Fact]
    public async Task Refresh_WithExpiredRefreshToken_ReturnsUnauthorized()
    {
        AuthResponse auth = await RegisterAsync("Expired Refresh User");
        await SetRefreshTokenExpiryAsync(auth.User.Email, DateTime.UtcNow.AddMinutes(-1));

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest
        {
            AccessToken = auth.AccessToken,
            RefreshToken = auth.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithReusedRefreshToken_ReturnsUnauthorized()
    {
        AuthResponse auth = await RegisterAsync("Reuse Refresh User");
        HttpResponseMessage firstRefresh = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest
        {
            AccessToken = auth.AccessToken,
            RefreshToken = auth.RefreshToken
        });

        firstRefresh.EnsureSuccessStatusCode();

        HttpResponseMessage secondRefresh = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest
        {
            AccessToken = auth.AccessToken,
            RefreshToken = auth.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, secondRefresh.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedRefreshToken()
    {
        AuthResponse auth = await RegisterAsync("Hashed Refresh User");

        AppUser user = await FindUserAsync(auth.User.Email);

        Assert.False(string.IsNullOrWhiteSpace(user.RefreshToken));
        Assert.NotEqual(auth.RefreshToken, user.RefreshToken);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsCurrentUser()
    {
        AuthResponse auth = await RegisterAsync("Current User", role: "Admin");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        HttpResponseMessage response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        UserInfo? user = await response.Content.ReadFromJsonAsync<UserInfo>();
        Assert.NotNull(user);
        Assert.Equal(auth.User.Email, user.Email);
        Assert.Equal("Admin", user.Role);
    }

    [Fact]
    public async Task Me_WithExpiredAccessToken_ReturnsUnauthorizedWithoutClockSkew()
    {
        using AuthApiFactory factory = AuthApiFactory.WithAccessTokenExpiration("0");
        using HttpClient client = factory.CreateClient();
        AuthResponse auth = await RegisterAsync(client, "Expired Access User");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        await Task.Delay(TimeSpan.FromSeconds(1.2));

        HttpResponseMessage response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidToken_ClearsRefreshToken()
    {
        AuthResponse auth = await RegisterAsync("Logout User");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        HttpResponseMessage response = await _client.PostAsync("/api/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        AppUser user = await FindUserAsync(auth.User.Email);
        Assert.Null(user.RefreshToken);
        Assert.Null(user.RefreshTokenExpiryTime);
    }

    [Fact]
    public async Task ProtectedController_WithoutToken_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/workorders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<AuthResponse> RegisterAsync(string displayName, string role = "Operator")
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = $"{displayName.Replace(" ", string.Empty).ToLowerInvariant()}-{Guid.NewGuid():N}@example.com",
            Password = "Test123!",
            DisplayName = displayName,
            Role = role
        });

        response.EnsureSuccessStatusCode();
        AuthResponse? auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth ?? throw new InvalidOperationException("Auth response was empty.");
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, string displayName, string role = "Operator")
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = $"{displayName.Replace(" ", string.Empty).ToLowerInvariant()}-{Guid.NewGuid():N}@example.com",
            Password = "Test123!",
            DisplayName = displayName,
            Role = role
        });

        response.EnsureSuccessStatusCode();
        AuthResponse? auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth ?? throw new InvalidOperationException("Auth response was empty.");
    }

    private async Task<AppUser> FindUserAsync(string email)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        UserManager<AppUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        AppUser? user = await userManager.FindByEmailAsync(email);
        return user ?? throw new InvalidOperationException($"User {email} was not found.");
    }

    private async Task SetRefreshTokenExpiryAsync(string email, DateTime expiry)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        MesDbContext context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
        AppUser user = context.Users.Single(user => user.Email == email);
        user.RefreshTokenExpiryTime = expiry;
        await context.SaveChangesAsync();
    }
}
