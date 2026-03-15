using System.Security.Claims;

namespace Forgefit.Api.Endpoints;

public static class WorkoutEndpoints
{
    public static IEndpointRouteBuilder MapWorkoutEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workouts")
            .WithTags("Workouts")
            .RequireAuthorization();

        group.MapGet("/", HandleGetAll)
            .Produces<List<WorkoutSession>>(200)
            .WithSummary("Get all workout sessions for the current user");

        group.MapGet("/today", HandleGetToday)
            .Produces<WorkoutSession>(200)
            .WithSummary("Get (or create) today's workout session");

        group.MapGet("/history/{exerciseId}", HandleGetHistory)
            .Produces<List<WorkoutSession>>(200)
            .WithSummary("Get past sessions containing sets for a specific exercise");

        group.MapPost("/sets", HandleAddSet)
            .Produces<WorkoutSet>(201)
            .ProducesProblem(400)
            .WithSummary("Add a set to today's workout session");

        group.MapDelete("/sets/{id}", HandleRemoveSet)
            .Produces(204)
            .ProducesProblem(404)
            .WithSummary("Remove a set by ID");

        return app;
    }

    private static async Task<IResult> HandleGetAll(
        ClaimsPrincipal principal,
        WorkoutService workoutService)
    {
        var userId = GetUserId(principal);
        var sessions = await workoutService.GetAllSessionsAsync(userId);
        return TypedResults.Ok(sessions);
    }

    private static async Task<IResult> HandleGetToday(
        ClaimsPrincipal principal,
        WorkoutService workoutService)
    {
        var userId = GetUserId(principal);
        var session = await workoutService.GetTodayAsync(userId);
        return TypedResults.Ok(session);
    }

    private static async Task<IResult> HandleGetHistory(
        string exerciseId,
        int limit,
        ClaimsPrincipal principal,
        WorkoutService workoutService)
    {
        var userId = GetUserId(principal);
        var sessions = await workoutService.GetHistoryAsync(userId, exerciseId, limit == 0 ? 3 : limit);
        return TypedResults.Ok(sessions);
    }

    private static async Task<IResult> HandleAddSet(
        CreateSetRequest req,
        ClaimsPrincipal principal,
        WorkoutService workoutService)
    {
        var userId = GetUserId(principal);
        var set = await workoutService.AddSetAsync(userId, req);
        return TypedResults.Created($"/api/workouts/sets/{set.Id}", set);
    }

    private static async Task<IResult> HandleRemoveSet(
        string id,
        ClaimsPrincipal principal,
        WorkoutService workoutService)
    {
        var userId = GetUserId(principal);
        await workoutService.RemoveSetAsync(id, userId);
        return TypedResults.NoContent();
    }

    private static string GetUserId(ClaimsPrincipal principal) =>
        principal.FindFirstValue("userId")
        ?? throw new HttpException(401, "Требуется авторизация");
}
