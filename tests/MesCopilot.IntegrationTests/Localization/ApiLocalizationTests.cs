using System.Net;
using System.Net.Http.Json;

namespace MesCopilot.IntegrationTests.Localization;

public sealed class ApiLocalizationTests :
    IClassFixture<Controllers.AgentApiFactory>,
    IClassFixture<Controllers.AuthApiFactory>
{
    private readonly Controllers.AgentApiFactory _factory;
    private readonly Controllers.AuthApiFactory _authFactory;

    public ApiLocalizationTests(
        Controllers.AgentApiFactory factory,
        Controllers.AuthApiFactory authFactory)
    {
        _factory = factory;
        _authFactory = authFactory;
    }

    [Theory]
    [InlineData("zh-CN", "消息不能为空")]
    [InlineData("en-US", "Message is required")]
    [InlineData("fr-FR", "消息不能为空")]
    public async Task AgentValidation_UsesLocalizedDetailAndStableCode(string language, string expectedDetail)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/agent/chat", new
        {
            mode = 0,
            message = ""
        });
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("MESSAGE_REQUIRED", problem!["code"].ToString());
        Assert.Contains(expectedDetail, problem["detail"].ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("zh-CN", "搜索查询不能为空")]
    [InlineData("en-US", "Search query is required")]
    public async Task AgentSearchValidation_UsesLocalizedDetailAndStableCode(
        string language,
        string expectedDetail)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);

        HttpResponseMessage response = await client.GetAsync("/api/agent/conversations/search?q=");
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("SEARCH_QUERY_REQUIRED", problem!["code"].ToString());
        Assert.Contains(expectedDetail, problem["detail"].ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("zh-CN", "邮箱或密码不正确")]
    [InlineData("en-US", "Invalid email or password")]
    public async Task LoginFailure_UsesLocalizedDetailAndStableCode(string language, string expectedDetail)
    {
        HttpClient client = _authFactory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = $"missing-{Guid.NewGuid():N}@example.com",
            password = "invalid-password"
        });
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_CREDENTIALS", problem!["code"].ToString());
        Assert.Contains(expectedDetail, problem["detail"].ToString(), StringComparison.Ordinal);
    }
}
