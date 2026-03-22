using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Forgefit.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", HandleRegister)
            .Produces<AuthResponse>(201)
            .ProducesProblem(400)
            .ProducesProblem(409)
            .WithSummary("Register a new user");

        group.MapPost("/login", HandleLogin)
            .Produces<AuthResponse>(200)
            .ProducesProblem(400)
            .ProducesProblem(401)
            .WithSummary("Login and receive a JWT token");

        group.MapGet("/me", HandleMe)
            .RequireAuthorization()
            .Produces<object>(200)
            .ProducesProblem(401)
            .WithSummary("Get the current authenticated user");

        return app;
    }

    private static async Task<IResult> HandleRegister(
        RegisterRequest req,
        AuthService authService)
    {
        var response = await authService.RegisterAsync(req);
        return TypedResults.Created("/api/auth/me", response);
    }

    private static async Task<IResult> HandleLogin(
        LoginRequest req,
        AuthService authService)
    {
        var response = await authService.LoginAsync(req);
        return TypedResults.Ok(response);
    }

    private static async Task<IResult> HandleMe(
        ClaimsPrincipal principal,
        AuthService authService)
    {
        var userId = principal.FindFirstValue("userId")
            ?? throw new HttpException(401, "Требуется авторизация");

        var user = await authService.GetCurrentUserAsync(userId);
        return TypedResults.Ok(new { user });
    }
}
