namespace Forgefit.Api.Infrastructure;

/// <summary>
/// Exception carrying an HTTP status code.
/// Caught by ErrorHandlingMiddleware and serialised to JSON.
/// </summary>
public sealed class HttpException(int statusCode, string message)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
