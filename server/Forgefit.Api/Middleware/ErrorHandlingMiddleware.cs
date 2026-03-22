using System.Text.Json;
using Forgefit.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Forgefit.Api.Middleware;

/// <summary>
/// Catches unhandled exceptions and converts them to RFC 7807 ProblemDetails JSON responses.
/// </summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (HttpException ex)
        {
            await WriteProblem(ctx, ex.StatusCode, GetTitle(ex.StatusCode), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteProblem(ctx, 500, "Internal Server Error", "Внутренняя ошибка сервера");
        }
    }

    private static string GetTitle(int status) => status switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        _ => "Error",
    };

    private static Task WriteProblem(HttpContext ctx, int status, string title, string detail)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json; charset=utf-8";

        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{status}",
            Title = title,
            Status = status,
            Detail = detail,
        };

        var body = JsonSerializer.Serialize(problem, _json);
        return ctx.Response.WriteAsync(body);
    }
}
