using Forgefit.Api.Models;

namespace Forgefit.Api.Database;

/// <summary>
/// Abstract database contract.
///
/// To plug in a real database, implement this interface and register
/// your class instead of InMemoryDatabase in Program.cs:
///
///   builder.Services.AddSingleton&lt;IDatabase, PostgresDatabase&gt;();
///   builder.Services.AddSingleton&lt;IDatabase, MongoDatabase&gt;();
///   builder.Services.AddScoped&lt;IDatabase, SqliteDatabase&gt;();
///
/// All methods are async to support both sync (in-memory) and async
/// (SQL, Mongo, etc.) backends transparently.
/// </summary>
public interface IDatabase
{
    // ── Users ─────────────────────────────────────────────────────────────────

    /// <summary>Find a user by email (case-insensitive). Returns null if not found.</summary>
    Task<User?> FindUserByEmailAsync(string email);

    /// <summary>Find a user by ID. Returns null if not found.</summary>
    Task<User?> FindUserByIdAsync(string id);

    /// <summary>Create and persist a new user record.</summary>
    Task<User> CreateUserAsync(string email, string passwordHash, string name);

    // ── Exercises ─────────────────────────────────────────────────────────────

    /// <summary>Return all exercises owned by the given user.</summary>
    Task<List<Exercise>> GetExercisesAsync(string userId);

    /// <summary>Return one exercise scoped to the owner. Null if not found or not owned.</summary>
    Task<Exercise?> GetExerciseByIdAsync(string id, string userId);

    /// <summary>Create a new exercise for the given user.</summary>
    Task<Exercise> CreateExerciseAsync(string userId, CreateExerciseRequest data);

    /// <summary>Update an exercise. Returns null if not found or not owned.</summary>
    Task<Exercise?> UpdateExerciseAsync(string id, string userId, UpdateExerciseRequest data);

    /// <summary>Delete an exercise. Returns false if not found or not owned.</summary>
    Task<bool> DeleteExerciseAsync(string id, string userId);

    // ── Workout sessions & sets ───────────────────────────────────────────────

    /// <summary>Return all sessions for the given user, newest first.</summary>
    Task<List<WorkoutSession>> GetSessionsAsync(string userId);

    /// <summary>Return the session for a specific ISO date or null.</summary>
    Task<WorkoutSession?> GetSessionByDateAsync(string userId, string date);

    /// <summary>Return the session for a specific date, creating it if it doesn't exist.</summary>
    Task<WorkoutSession> GetOrCreateSessionByDateAsync(string userId, string date);

    /// <summary>Append a set to the given session. Returns the created WorkoutSet.</summary>
    Task<WorkoutSet> AddSetAsync(string sessionId, string userId, CreateSetRequest data);

    /// <summary>Remove a set by ID. Returns false if not found.</summary>
    Task<bool> RemoveSetAsync(string setId, string userId);

    /// <summary>
    /// Return up to <paramref name="limit"/> past sessions (excluding today)
    /// that contain at least one set for the given exercise, newest first.
    /// Each returned session contains only the sets for that exercise.
    /// </summary>
    Task<List<WorkoutSession>> GetHistoryForExerciseAsync(
        string userId, string exerciseId, int limit);
}
