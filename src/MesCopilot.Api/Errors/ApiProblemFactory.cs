using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace MesCopilot.Api.Errors;

public sealed class ApiProblemFactory
{
    private readonly IStringLocalizer _localizer;

    public ApiProblemFactory(IStringLocalizerFactory factory)
    {
        _localizer = factory.Create("SharedResource", "MesCopilot.Api");
    }

    public ApiProblemDetails Create(int status, string code, string titleKey, string detailKey)
    {
        ApiProblemDetails problem = new()
        {
            Status = status,
            Title = Get(titleKey),
            Detail = Get(detailKey),
            Type = $"https://httpstatuses.com/{status}",
            Code = code
        };
        return problem;
    }

    public string GetMessage(string key) => Get(key);

    private string Get(string key)
    {
        LocalizedString value = _localizer[key];
        if (value.ResourceNotFound)
        {
            throw new InvalidOperationException($"Localization resource '{key}' is missing.");
        }
        return value.Value;
    }
}

public sealed class ApiProblemDetails : ProblemDetails
{
    public string Code { get; set; } = string.Empty;
}
