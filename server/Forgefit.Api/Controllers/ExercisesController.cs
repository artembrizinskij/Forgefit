using System.Security.Claims;
using Forgefit.Api.Database;
using Forgefit.Api.Infrastructure;
using Forgefit.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forgefit.Api.Controllers;

[ApiController]
[Route("api/exercises")]
[Authorize]
public sealed class ExercisesController(IDatabase db) : ControllerBase
{
    // ── GET /api/exercises ────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var exercises = await db.GetExercisesAsync(GetUserId());
        return Ok(exercises);
    }

    // ── GET /api/exercises/:id ────────────────────────────────────────────────

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var exercise = await db.GetExerciseByIdAsync(id, GetUserId())
            ?? throw new HttpException(404, "Упражнение не найдено");

        return Ok(exercise);
    }

    // ── POST /api/exercises ───────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExerciseRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new HttpException(400, "Название упражнения обязательно");
        if (string.IsNullOrWhiteSpace(req.Type))
            throw new HttpException(400, "Тип упражнения обязателен");
        if (string.IsNullOrWhiteSpace(req.MuscleGroup))
            throw new HttpException(400, "Группа мышц обязательна");
        if (req.Params is null)
            throw new HttpException(400, "Параметры упражнения обязательны");

        var exercise = await db.CreateExerciseAsync(GetUserId(), req);
        return StatusCode(201, exercise);
    }

    // ── PUT /api/exercises/:id ────────────────────────────────────────────────

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateExerciseRequest req)
    {
        if (req.Name is not null && string.IsNullOrWhiteSpace(req.Name))
            throw new HttpException(400, "Название не может быть пустым");

        var updated = await db.UpdateExerciseAsync(id, GetUserId(), req)
            ?? throw new HttpException(404, "Упражнение не найдено");

        return Ok(updated);
    }

    // ── DELETE /api/exercises/:id ─────────────────────────────────────────────

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await db.DeleteExerciseAsync(id, GetUserId());
        if (!deleted) throw new HttpException(404, "Упражнение не найдено");
        return NoContent();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private string GetUserId() =>
        User.FindFirstValue("userId")
        ?? throw new HttpException(401, "Требуется авторизация");
}
