using System.Diagnostics;
using System.Security.Claims;
using MesCopilot.Domain.Entities.Auditing;
using MesCopilot.Infrastructure.Auditing;
using MesCopilot.Infrastructure.Data;
using MesCopilot.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Routing;

namespace MesCopilot.Api.Middleware;

public sealed class AuditMiddleware
{
    private static readonly string[] ExcludedBodyPrefixes =
    [
        "/api/auth",
        "/api/device-connections"
    ];

    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AuditRedactor _redactor;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        AuditRedactor redactor,
        ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _redactor = redactor;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        AuditRequestContext requestContext,
        ITenantContext tenantContext)
    {
        string correlationId = context.TraceIdentifier;
        string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        requestContext.Initialize(userId, correlationId);
        string? requestBody = await ReadRequestBodyAsync(context);
        long started = Stopwatch.GetTimestamp();

        try
        {
            await _next(context);
        }
        finally
        {
            long durationMilliseconds = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            await TryWriteAuditAsync(context, tenantContext, requestBody, correlationId, durationMilliseconds);
        }
    }

    private async Task TryWriteAuditAsync(
        HttpContext context,
        ITenantContext tenantContext,
        string? requestBody,
        string correlationId,
        long durationMilliseconds)
    {
        try
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            IAuditLogWriter writer = scope.ServiceProvider.GetRequiredService<IAuditLogWriter>();
            string tenantId = tenantContext.TenantId ?? SeedData.DefaultTenantId;
            RouteEndpoint? endpoint = context.GetEndpoint() as RouteEndpoint;
            AuditLog auditLog = new()
            {
                TenantId = tenantId,
                UserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier),
                Method = context.Request.Method,
                RouteTemplate = endpoint?.RoutePattern.RawText ?? context.Request.Path.Value ?? string.Empty,
                QueryKeys = string.Join(",", context.Request.Query.Keys.Order(StringComparer.Ordinal)),
                StatusCode = context.Response.StatusCode,
                DurationMilliseconds = durationMilliseconds,
                RequestBody = requestBody,
                CorrelationId = correlationId,
                IpHash = _redactor.HashIp(context.Connection.RemoteIpAddress?.ToString())
            };
            await writer.WriteAsync(auditLog, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to persist HTTP audit log for {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);
        }
    }

    private async Task<string?> ReadRequestBodyAsync(HttpContext context)
    {
        if (context.Request.ContentLength is null or 0 ||
            context.Request.ContentType is null ||
            !context.Request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) ||
            ExcludedBodyPrefixes.Any(prefix => context.Request.Path.StartsWithSegments(prefix)))
        {
            return null;
        }

        context.Request.EnableBuffering();
        using StreamReader reader = new(
            context.Request.Body,
            leaveOpen: true);
        string body = await reader.ReadToEndAsync(context.RequestAborted);
        context.Request.Body.Position = 0;
        return _redactor.RedactJson(body);
    }
}
