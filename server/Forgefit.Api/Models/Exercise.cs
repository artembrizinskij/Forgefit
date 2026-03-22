namespace Forgefit.Api.Models;

/// <summary>
/// Exercise parameter toggles. Property names are camelCase in JSON
/// (configured globally in Program.cs) to match the Vue frontend.
/// </summary>
public class ExerciseParams
{
    // ── Strength ──────────────────────────────────────────────────────────
    public bool Weight { get; set; }
    public bool Reps { get; set; }
    public bool Tut { get; set; }          // Time Under Tension
    public bool Rest { get; set; }
    public int RestDuration { get; set; } = 90; // seconds
    public bool Rpe { get; set; }          // Rate of Perceived Exertion

    // ── Cardio ────────────────────────────────────────────────────────────
    public bool Duration { get; set; }
    public bool Pace { get; set; }
    public bool Distance { get; set; }
    public bool Incline { get; set; }
    public bool HeartRate { get; set; }
    public string Units { get; set; } = "kmh"; // "kmh" | "minpkm"

    // ── Stretch ───────────────────────────────────────────────────────────
    public bool Side { get; set; }
    public bool Breathing { get; set; }
}

public class Exercise
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;
    public List<string> Muscles { get; set; } = [];
    public string Description { get; set; } = string.Empty;

    /// <summary>"strength" | "cardio" | "stretch"</summary>
    public string Type { get; set; } = string.Empty;

    public ExerciseParams Params { get; set; } = new();
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}
