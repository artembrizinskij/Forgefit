namespace Forgefit.Api.Services;

/// <summary>Manages workout sessions and sets.</summary>
public sealed class WorkoutService(IDatabase db)
{
    private static string TodayIso() => DateTime.UtcNow.ToString("yyyy-MM-dd");

    public Task<List<WorkoutSession>> GetAllSessionsAsync(string userId) =>
        db.GetSessionsAsync(userId);

    public Task<WorkoutSession> GetTodayAsync(string userId) =>
        db.GetOrCreateSessionByDateAsync(userId, TodayIso());

    public async Task<List<WorkoutSession>> GetHistoryAsync(string userId, string exerciseId, int limit)
    {
        limit = Math.Clamp(limit, 1, 20);
        return await db.GetHistoryForExerciseAsync(userId, exerciseId, limit);
    }

    public async Task<WorkoutSet> AddSetAsync(string userId, CreateSetRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ExerciseId))
            throw new HttpException(400, "exerciseId обязателен");

        var session = await db.GetOrCreateSessionByDateAsync(userId, TodayIso());
        return await db.AddSetAsync(session.Id, userId, req);
    }

    public async Task RemoveSetAsync(string id, string userId)
    {
        var removed = await db.RemoveSetAsync(id, userId);
        if (!removed)
            throw new HttpException(404, "Подход не найден");
    }
}
