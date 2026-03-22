namespace Forgefit.Api.Services;

/// <summary>CRUD operations for exercises, scoped to the requesting user.</summary>
public sealed class ExerciseService(IDatabase db)
{
    public Task<List<Exercise>> GetAllAsync(string userId) =>
        db.GetExercisesAsync(userId);

    public async Task<Exercise> GetByIdAsync(string id, string userId)
    {
        var exercise = await db.GetExerciseByIdAsync(id, userId);
        return exercise ?? throw new HttpException(404, "Упражнение не найдено");
    }

    public async Task<Exercise> CreateAsync(string userId, CreateExerciseRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new HttpException(400, "Название упражнения обязательно");
        if (string.IsNullOrWhiteSpace(req.Type))
            throw new HttpException(400, "Тип упражнения обязателен");
        if (string.IsNullOrWhiteSpace(req.MuscleGroup))
            throw new HttpException(400, "Группа мышц обязательна");
        if (req.Params is null)
            throw new HttpException(400, "Параметры упражнения обязательны");

        return await db.CreateExerciseAsync(userId, req);
    }

    public async Task<Exercise> UpdateAsync(string id, string userId, UpdateExerciseRequest req)
    {
        if (req.Name is not null && string.IsNullOrWhiteSpace(req.Name))
            throw new HttpException(400, "Название не может быть пустым");

        var updated = await db.UpdateExerciseAsync(id, userId, req);
        return updated ?? throw new HttpException(404, "Упражнение не найдено");
    }

    public async Task DeleteAsync(string id, string userId)
    {
        var deleted = await db.DeleteExerciseAsync(id, userId);
        if (!deleted)
            throw new HttpException(404, "Упражнение не найдено");
    }
}
