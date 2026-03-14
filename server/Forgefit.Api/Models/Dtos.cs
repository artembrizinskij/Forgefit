using Forgefit.Api.Models;

namespace Forgefit.Api.Models;

// ── Auth ──────────────────────────────────────────────────────────────────────

public record RegisterRequest(string Email, string Password, string Name);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token, PublicUser User);

// ── Exercise ──────────────────────────────────────────────────────────────────

public class CreateExerciseRequest
{
    public string Name { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;
    public List<string>? Muscles { get; set; }
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public ExerciseParams? Params { get; set; }
}

public class UpdateExerciseRequest
{
    public string? Name { get; set; }
    public string? MuscleGroup { get; set; }
    public List<string>? Muscles { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public ExerciseParams? Params { get; set; }
}

// ── Workout ───────────────────────────────────────────────────────────────────

public class CreateSetRequest
{
    public string ExerciseId { get; set; } = string.Empty;
    public double? Weight { get; set; }
    public int? Reps { get; set; }
    public int? Tut { get; set; }
    public double? Rpe { get; set; }
    public double? Duration { get; set; }
    public double? Pace { get; set; }
    public double? Distance { get; set; }
    public double? Incline { get; set; }
    public int? HeartRate { get; set; }
    public string? Side { get; set; }
    public int? BreathingCycles { get; set; }
    public long? Timestamp { get; set; }
}

// ── Shared ────────────────────────────────────────────────────────────────────

public record ErrorResponse(string Error);
