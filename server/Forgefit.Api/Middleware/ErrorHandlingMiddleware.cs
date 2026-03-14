using System.Text.Json;
using Forgefit.Api.Infrastructure;

namespace Forgefit.Api.Middleware;

/// <summary>
/// Catches unhandled exceptions and converts them to structured JSON responses:
///   { "error": "message" }
/// </summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (HttpException ex)
        {
            await WriteError(ctx, ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteError(ctx, 500, "Внутренняя ошибка сервера");
        }
    }

    private static Task WriteError(HttpContext ctx, int status, string message)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        var body = JsonSerializer.Serialize(new { error = message }, _json);
        return ctx.Response.WriteAsync(body);
    }
}
