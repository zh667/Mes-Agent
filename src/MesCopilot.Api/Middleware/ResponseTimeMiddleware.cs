using System.Diagnostics;
using System.Globalization;

namespace MesCopilot.Api.Middleware;

public class ResponseTimeMiddleware
{
    public const string HeaderName = "X-Response-Time-ms";

    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseTimeMiddleware> _logger;

    public ResponseTimeMiddleware(
        RequestDelegate next,
        ILogger<ResponseTimeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        context.Response.OnStarting(() =>
        {
            stopwatch.Stop();
            string elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture);
            context.Response.Headers[HeaderName] = elapsedMilliseconds;
            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsedMilliseconds);
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
