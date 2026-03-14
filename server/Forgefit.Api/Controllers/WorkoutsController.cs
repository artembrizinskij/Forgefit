using System.Security.Claims;
using Forgefit.Api.Database;
using Forgefit.Api.Infrastructure;
using Forgefit.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forgefit.Api.Controllers;

[ApiController]
[Route("api/workouts")]
[Authorize]
public sealed class WorkoutsController(IDatabase db) : ControllerBase
{
    private static string TodayIso() => DateTime.UtcNow.ToString("yyyy-MM-dd");

    // ── GET /api/workouts ─────────────────────────────────────────────────────
    // Returns all sessions for the current user, newest first.

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var sessions = await db.GetSessionsAsync(GetUserId());
        return Ok(sessions);
    }

    // ── GET /api/workouts/today ───────────────────────────────────────────────
    // Returns today's session, creating it on the server if it doesn't exist.

    [HttpGet("today")]
    public async Task<IActionResult> GetToday()
    {
        var session = await db.GetOrCreateSessionByDateAsync(GetUserId(), TodayIso());
        return Ok(session);
    }

    // ── GET /api/workouts/history/{exerciseId} ────────────────────────────────
    // Returns past sessions (up to `limit`) containing sets for the exercise.

    [HttpGet("history/{exerciseId}")]
    public async Task<IActionResult> GetHistory(string exerciseId, [FromQuery] int limit = 3)
    {
        limit = Math.Clamp(limit, 1, 20);
        var sessions = await db.GetHistoryForExerciseAsync(GetUserId(), exerciseId, limit);
        return Ok(sessions);
    }

    // ── POST /api/workouts/sets ───────────────────────────────────────────────
    // Adds a set to today's session.

    [HttpPost("sets")]
    public async Task<IActionResult> AddSet([FromBody] CreateSetRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ExerciseId))
            throw new HttpException(400, "exerciseId обязателен");

        var session = await db.GetOrCreateSessionByDateAsync(GetUserId(), TodayIso());
        var set = await db.AddSetAsync(session.Id, GetUserId(), req);
        return StatusCode(201, set);
    }

    // ── DELETE /api/workouts/sets/{id} ────────────────────────────────────────

    [HttpDelete("sets/{id}")]
    public async Task<IActionResult> RemoveSet(string id)
    {
        var removed = await db.RemoveSetAsync(id, GetUserId());
        if (!removed) throw new HttpException(404, "Подход не найден");
        return NoContent();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private string GetUserId() =>
        User.FindFirstValue("userId")
        ?? throw new HttpException(401, "Требуется авторизация");
}
