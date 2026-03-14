namespace Forgefit.Api.Models;

public class WorkoutSet
{
    public string Id { get; set; } = string.Empty;
    public string ExerciseId { get; set; } = string.Empty;

    // ── Strength ──────────────────────────────────────────────────────────
    public double? Weight { get; set; }
    public int? Reps { get; set; }
    public int? Tut { get; set; }
    public double? Rpe { get; set; }

    // ── Cardio ────────────────────────────────────────────────────────────
    public double? Duration { get; set; }
    public double? Pace { get; set; }
    public double? Distance { get; set; }
    public double? Incline { get; set; }
    public int? HeartRate { get; set; }

    // ── Stretch ───────────────────────────────────────────────────────────
    /// <summary>"left" | "right" | "both"</summary>
    public string? Side { get; set; }
    public int? BreathingCycles { get; set; }

    /// <summary>Unix timestamp in milliseconds.</summary>
    public long Timestamp { get; set; }
}

public class WorkoutSession
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;

    /// <summary>ISO date string: "YYYY-MM-DD".</summary>
    public string Date { get; set; } = string.Empty;

    public List<WorkoutSet> Sets { get; set; } = [];
    public string CreatedAt { get; set; } = string.Empty;
}
