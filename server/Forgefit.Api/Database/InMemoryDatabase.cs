using Forgefit.Api.Models;

namespace Forgefit.Api.Database;

/// <summary>
/// In-memory database implementation.
/// All data is stored in process memory — data is lost on server restart.
/// Intended for development and initial deployment; replace with a real
/// connector by implementing IDatabase and updating Program.cs.
/// </summary>
public sealed class InMemoryDatabase : IDatabase
{
    private readonly List<User> _users = [];
    private readonly List<Exercise> _exercises = [];
    private readonly List<WorkoutSession> _sessions = [];
    private readonly object _lock = new();

    private static string NewId() => Guid.NewGuid().ToString();
    private static string NowIso() => DateTime.UtcNow.ToString("O");
    private static string TodayIso() => DateTime.UtcNow.ToString("yyyy-MM-dd");

    // ── Users ─────────────────────────────────────────────────────────────────

    public Task<User?> FindUserByEmailAsync(string email)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(u =>
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult<User?>(user);
        }
    }

    public Task<User?> FindUserByIdAsync(string id)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            return Task.FromResult<User?>(user);
        }
    }

    public Task<User> CreateUserAsync(string email, string passwordHash, string name)
    {
        var user = new User
        {
            Id = NewId(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Name = name,
            CreatedAt = NowIso(),
        };
        lock (_lock) { _users.Add(user); }
        return Task.FromResult(user);
    }

    // ── Exercises ─────────────────────────────────────────────────────────────

    public Task<List<Exercise>> GetExercisesAsync(string userId)
    {
        lock (_lock)
        {
            var result = _exercises.Where(e => e.UserId == userId).ToList();
            return Task.FromResult(result);
        }
    }

    public Task<Exercise?> GetExerciseByIdAsync(string id, string userId)
    {
        lock (_lock)
        {
            var ex = _exercises.FirstOrDefault(e => e.Id == id && e.UserId == userId);
            return Task.FromResult<Exercise?>(ex);
        }
    }

    public Task<Exercise> CreateExerciseAsync(string userId, CreateExerciseRequest data)
    {
        var now = NowIso();
        var exercise = new Exercise
        {
            Id = NewId(),
            UserId = userId,
            Name = data.Name.Trim(),
            MuscleGroup = data.MuscleGroup,
            Muscles = data.Muscles ?? [],
            Description = data.Description ?? string.Empty,
            Type = data.Type,
            Params = data.Params ?? new ExerciseParams(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        lock (_lock) { _exercises.Add(exercise); }
        return Task.FromResult(exercise);
    }

    public Task<Exercise?> UpdateExerciseAsync(string id, string userId, UpdateExerciseRequest data)
    {
        lock (_lock)
        {
            var ex = _exercises.FirstOrDefault(e => e.Id == id && e.UserId == userId);
            if (ex is null) return Task.FromResult<Exercise?>(null);

            if (data.Name is not null) ex.Name = data.Name.Trim();
            if (data.MuscleGroup is not null) ex.MuscleGroup = data.MuscleGroup;
            if (data.Muscles is not null) ex.Muscles = data.Muscles;
            if (data.Description is not null) ex.Description = data.Description;
            if (data.Type is not null) ex.Type = data.Type;
            if (data.Params is not null) ex.Params = data.Params;
            ex.UpdatedAt = NowIso();

            return Task.FromResult<Exercise?>(ex);
        }
    }

    public Task<bool> DeleteExerciseAsync(string id, string userId)
    {
        lock (_lock)
        {
            var ex = _exercises.FirstOrDefault(e => e.Id == id && e.UserId == userId);
            if (ex is null) return Task.FromResult(false);
            _exercises.Remove(ex);
            return Task.FromResult(true);
        }
    }

    // ── Workout sessions & sets ───────────────────────────────────────────────

    public Task<List<WorkoutSession>> GetSessionsAsync(string userId)
    {
        lock (_lock)
        {
            var result = _sessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.Date)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<WorkoutSession?> GetSessionByDateAsync(string userId, string date)
    {
        lock (_lock)
        {
            var session = _sessions.FirstOrDefault(s => s.UserId == userId && s.Date == date);
            return Task.FromResult<WorkoutSession?>(session);
        }
    }

    public Task<WorkoutSession> GetOrCreateSessionByDateAsync(string userId, string date)
    {
        lock (_lock)
        {
            var session = _sessions.FirstOrDefault(s => s.UserId == userId && s.Date == date);
            if (session is not null) return Task.FromResult(session);

            session = new WorkoutSession
            {
                Id = NewId(),
                UserId = userId,
                Date = date,
                Sets = [],
                CreatedAt = NowIso(),
            };
            _sessions.Add(session);
            return Task.FromResult(session);
        }
    }

    public Task<WorkoutSet> AddSetAsync(string sessionId, string userId, CreateSetRequest data)
    {
        lock (_lock)
        {
            var session = _sessions.FirstOrDefault(s => s.Id == sessionId && s.UserId == userId)
                ?? throw new InvalidOperationException("Session not found");

            var set = new WorkoutSet
            {
                Id = NewId(),
                ExerciseId = data.ExerciseId,
                Timestamp = data.Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Weight = data.Weight,
                Reps = data.Reps,
                Tut = data.Tut,
                Rpe = data.Rpe,
                Duration = data.Duration,
                Pace = data.Pace,
                Distance = data.Distance,
                Incline = data.Incline,
                HeartRate = data.HeartRate,
                Side = data.Side,
                BreathingCycles = data.BreathingCycles,
            };

            session.Sets.Add(set);
            return Task.FromResult(set);
        }
    }

    public Task<bool> RemoveSetAsync(string setId, string userId)
    {
        lock (_lock)
        {
            var session = _sessions.FirstOrDefault(s =>
                s.UserId == userId && s.Sets.Any(st => st.Id == setId));

            if (session is null) return Task.FromResult(false);

            var set = session.Sets.FirstOrDefault(st => st.Id == setId);
            if (set is null) return Task.FromResult(false);

            session.Sets.Remove(set);
            return Task.FromResult(true);
        }
    }

    public Task<List<WorkoutSession>> GetHistoryForExerciseAsync(
        string userId, string exerciseId, int limit)
    {
        var today = TodayIso();
        lock (_lock)
        {
            var result = _sessions
                .Where(s =>
                    s.UserId == userId &&
                    s.Date != today &&
                    s.Sets.Any(st => st.ExerciseId == exerciseId))
                .OrderByDescending(s => s.Date)
                .Take(limit)
                .Select(s => new WorkoutSession
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    Date = s.Date,
                    CreatedAt = s.CreatedAt,
                    // Return only sets for the requested exercise
                    Sets = s.Sets.Where(st => st.ExerciseId == exerciseId).ToList(),
                })
                .ToList();

            return Task.FromResult(result);
        }
    }
}
