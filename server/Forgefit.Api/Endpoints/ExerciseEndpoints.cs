using System.Security.Claims;

namespace Forgefit.Api.Endpoints;

public static class ExerciseEndpoints
{
    public static IEndpointRouteBuilder MapExerciseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/exercises")
            .WithTags("Exercises")
            .RequireAuthorization();

        group.MapGet("/", HandleGetAll)
            .Produces<List<Exercise>>(200)
            .WithSummary("Get all exercises for the current user");

        group.MapGet("/{id}", HandleGetById)
            .Produces<Exercise>(200)
            .ProducesProblem(404)
            .WithSummary("Get an exercise by ID");

        group.MapPost("/", HandleCreate)
            .Produces<Exercise>(201)
            .ProducesProblem(400)
            .WithSummary("Create a new exercise");

        group.MapPut("/{id}", HandleUpdate)
            .Produces<Exercise>(200)
            .ProducesProblem(400)
            .ProducesProblem(404)
            .WithSummary("Update an existing exercise");

        group.MapDelete("/{id}", HandleDelete)
            .Produces(204)
            .ProducesProblem(404)
            .WithSummary("Delete an exercise");

        return app;
    }

    private static async Task<IResult> HandleGetAll(
        ClaimsPrincipal principal,
        ExerciseService exerciseService)
    {
        var userId = GetUserId(principal);
        var exercises = await exerciseService.GetAllAsync(userId);
        return TypedResults.Ok(exercises);
    }

    private static async Task<IResult> HandleGetById(
        string id,
        ClaimsPrincipal principal,
        ExerciseService exerciseService)
    {
        var userId = GetUserId(principal);
        var exercise = await exerciseService.GetByIdAsync(id, userId);
        return TypedResults.Ok(exercise);
    }

    private static async Task<IResult> HandleCreate(
        CreateExerciseRequest req,
        ClaimsPrincipal principal,
        ExerciseService exerciseService)
    {
        var userId = GetUserId(principal);
        var exercise = await exerciseService.CreateAsync(userId, req);
        return TypedResults.Created($"/api/exercises/{exercise.Id}", exercise);
    }

    private static async Task<IResult> HandleUpdate(
        string id,
        UpdateExerciseRequest req,
        ClaimsPrincipal principal,
        ExerciseService exerciseService)
    {
        var userId = GetUserId(principal);
        var updated = await exerciseService.UpdateAsync(id, userId, req);
        return TypedResults.Ok(updated);
    }

    private static async Task<IResult> HandleDelete(
        string id,
        ClaimsPrincipal principal,
        ExerciseService exerciseService)
    {
        var userId = GetUserId(principal);
        await exerciseService.DeleteAsync(id, userId);
        return TypedResults.NoContent();
    }

    private static string GetUserId(ClaimsPrincipal principal) =>
        principal.FindFirstValue("userId")
        ?? throw new HttpException(401, "Требуется авторизация");
}
