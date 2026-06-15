using System.Net;
using System.Text.Json;

namespace Pigment.API.Middleware;

/// <summary>
/// Catches all unhandled exceptions and returns a consistent RFC-7807 ProblemDetails response.
/// Must be registered first in the middleware pipeline.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request cancelled by client.");
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Bad request: {Message}", ex.Message);
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, "Bad Request", ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
            await WriteProblemAsync(context, HttpStatusCode.NotFound, "Not Found", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorised: {Message}", ex.Message);
            await WriteProblemAsync(context, HttpStatusCode.Forbidden, "Forbidden", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteProblemAsync(
                context,
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please try again or contact support.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string title,
        string detail)
    {
        context.Response.StatusCode  = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type     = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status   = (int)statusCode,
            detail,
            instance = context.Request.Path.Value
        };

        var json = JsonSerializer.Serialize(problem,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(json);
    }
}
