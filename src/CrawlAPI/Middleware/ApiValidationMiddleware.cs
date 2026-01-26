using CrawlAPI.Services.Internal;
using Microsoft.AspNetCore.Mvc;

namespace CrawlAPI.Controllers;

/// <summary>
/// Middleware for request validation and error handling.
/// </summary>
public class ApiValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiValidationMiddleware> _logger;

    public ApiValidationMiddleware(RequestDelegate next, ILogger<ApiValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "Internal server error" });
        }
    }
}
