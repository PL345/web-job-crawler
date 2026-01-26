using System.Diagnostics;

namespace CrawlAPI.Middleware;

/// <summary>
/// Logs HTTP request/response timing for performance monitoring.
/// </summary>
public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var watch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            watch.Stop();
            _logger.LogInformation(
                "Request {Method} {Path} completed in {ElapsedMilliseconds}ms with status {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                watch.ElapsedMilliseconds,
                context.Response.StatusCode
            );
        }
    }
}
