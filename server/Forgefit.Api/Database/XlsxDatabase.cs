using System.Text.Json;
using ClosedXML.Excel;
using Forgefit.Api.Models;

namespace Forgefit.Api.Database;

/// <summary>
/// Excel-backed database. Layout:
///
///   data/_users.xlsx                — global users registry
///     Sheet "Users"                 — one row per user
///
///   data/{userId}.xlsx              — all data for one user
///     Sheet "_Exercises"            — exercise catalogue
///     Sheet "_Sessions"             — workout session headers
///     Sheet "{ExerciseName}"        — one sheet per exercise, all sets
///
/// To switch back to in-memory set env DB_TYPE=memory.
/// The data directory is controlled by DB_DATA_DIR (default: "data").
/// </summary>
public sealed class XlsxDatabase : IDatabase
{
    private readonly string _dataDir;
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    // ── Column constants (1-based) ────────────────────────────────────────────

    private static class UC  { public const int Id=1, Email=2, Hash=3, Name=4, CreatedAt=5; }
    private static class EC  { public const int Id=1, Name=2, MuscleGroup=3, Muscles=4, Description=5, Type=6, ParamsJson=7, CreatedAt=8, UpdatedAt=9, SheetName=10; }
    private static class SC  { public const int Id=1, Date=2, CreatedAt=3; }
    private static class SetC{ public const int SetId=1, SessionId=2, Date=3, Timestamp=4, Weight=5, Reps=6, Tut=7, Rpe=8, Duration=9, Pace=10, Distance=11, Incline=12, HeartRate=13, Side=14, BreathingCycles=15; }

    private static readonly string[] UserHeaders      = ["Id", "Email", "PasswordHash", "Name", "CreatedAt"];
    private static readonly string[] ExerciseHeaders  = ["Id", "Name", "MuscleGroup", "Muscles", "Description", "Type", "ParamsJson", "CreatedAt", "UpdatedAt", "SheetName"];
    private static readonly string[] SessionHeaders   = ["Id", "Date", "CreatedAt"];
    private static readonly string[] SetHeaders       = ["SetId", "SessionId", "Date", "Timestamp", "Weight", "Reps", "TUT", "RPE", "Duration", "Pace", "Distance", "Incline", "HeartRate", "Side", "BreathingCycles"];

    // ── Init ──────────────────────────────────────────────────────────────────

    public XlsxDatabase(string dataDir = "data")
    {
        _dataDir = dataDir;
        Directory.CreateDirectory(_dataDir);
        lock (_lock) { EnsureUsersFile(); }
    }

    // ── Paths ─────────────────────────────────────────────────────────────────

    private string UsersFilePath        => Path.Combine(_dataDir, "_users.xlsx");
    private string UserFilePath(string userId) => Path.Combine(_dataDir, $"{userId}.xlsx");

    // ── Statics ───────────────────────────────────────────────────────────────

    private static string NewId()    => Guid.NewGuid().ToString();
    private static string NowIso()   => DateTime.UtcNow.ToString("O");
    private static string TodayIso() => DateTime.UtcNow.ToString("yyyy-MM-dd");

    private static int LastDataRow(IXLWorksheet ws)
        => ws.LastRowUsed()?.RowNumber() ?? 1;

    // ── IDatabase: Users ──────────────────────────────────────────────────────

    public Task<User?> FindUserByEmailAsync(string email)
    {
        lock (_lock)
        {
            using var wb = new XLWorkbook(UsersFilePath);
            var ws = wb.Worksheet("Users");
            for (int r = 2; r <= LastDataRow(ws); r++)
            {
                var stored = CellStr(ws.Cell(r, UC.Email));
                if (stored is not null && stored.Equals(email, StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult<User?>(ReadUser(ws, r));
            }
            return Task.FromResult<User?>(null);
        }
    }

    public Task<User?> FindUserByIdAsync(string id)
    {
        lock (_lock)
        {
            using var wb = new XLWorkbook(UsersFilePath);
            var ws = wb.Worksheet("Users");
            for (int r = 2; r <= LastDataRow(ws); r++)
            {
                if (CellStr(ws.Cell(r, UC.Id)) == id)
                    return Task.FromResult<User?>(ReadUser(ws, r));
            }
            return Task.FromResult<User?>(null);
        }
    }

    public Task<User> CreateUserAsync(string email, string passwordHash, string name)
    {
        var user = new User
        {
            Id          = NewId(),
            Email       = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Name        = name,
            CreatedAt   = NowIso(),
        };
        lock (_lock)
        {
            using var wb = new XLWorkbook(UsersFilePath);
            AppendRow(wb.Worksheet("Users"), user.Id, user.Email, user.PasswordHash, user.Name, user.CreatedAt);
            wb.Save();
            EnsureUserFile(user.Id);
        }
        return Task.FromResult(user);
    }

    private static User ReadUser(IXLWorksheet ws, int row) => new()
    {
        Id           = CellStr(ws.Cell(row, UC.Id))        ?? "",
        Email        = CellStr(ws.Cell(row, UC.Email))     ?? "",
        PasswordHash = CellStr(ws.Cell(row, UC.Hash))      ?? "",
        Name         = CellStr(ws.Cell(row, UC.Name))      ?? "",
        CreatedAt    = CellStr(ws.Cell(row, UC.CreatedAt)) ?? "",
    };

    // ── IDatabase: Exercises ──────────────────────────────────────────────────

    public Task<List<Exercise>> GetExercisesAsync(string userId)
    {
        lock (_lock)
        {
            EnsureUserFile(userId);
            using var wb = new XLWorkbook(UserFilePath(userId));
            var ws   = wb.Worksheet("_Exercises");
            var list = new List<Exercise>();
            for (int r = 2; r <= LastDataRow(ws); r++)
            {
                var ex = ReadExercise(ws, r, userId);
                if (ex is not null) list.Add(ex);
            }
            return Task.FromResult(list);
        }
    }

    public Task<Exercise?> GetExerciseByIdAsync(string id, string userId)
    {
        lock (_lock)
        {
            EnsureUserFile(userId);
            using var wb = new XLWorkbook(UserFilePath(userId));
            var (row, _) = FindExerciseRow(wb.Worksheet("_Exercises"), id);
            return Task.FromResult(row > 0 ? ReadExercise(wb.Worksheet("_Exercises"), row, userId) : null);
        }
    }

    public Task<Exercise> CreateExerciseAsync(string userId, CreateExerciseRequest data)
    {
        var now = NowIso();
        var exercise = new Exercise
        {
            Id          = NewId(),
            UserId      = userId,
            Name        = data.Name.Trim(),
            MuscleGroup = data.MuscleGroup,
            Muscles     = data.Muscles ?? [],
            Description = data.Description ?? string.Empty,
            Type        = data.Type,
            Params      = data.Params ?? new ExerciseParams(),
            CreatedAt   = now,
            UpdatedAt   = now,
        };
        lock (_lock)
        {
            EnsureUserFile(userId);
            using var wb = new XLWorkbook(UserFilePath(userId));

            // create per-exercise sheet
            var sheetName = UniqueSheetName(wb, SafeSheetName(exercise.Name));
            var setSheet  = wb.Worksheets.Add(sheetName);
            WriteSetSheetHeader(setSheet);

            // append to exercise catalogue
            AppendExerciseRow(wb.Worksheet("_Exercises"), exercise, sheetName);
            wb.Save();
        }
        return Task.FromResult(exercise);
    }

    public Task<Exercise?> UpdateExerciseAsync(string id, string userId, UpdateExerciseRequest data)
    {
        lock (_lock)
        {
            EnsureUserFile(userId);
            using var wb = new XLWorkbook(UserFilePath(userId));
            var ws = wb.Worksheet("_Exercises");
            var (row, sheetName) = FindExerciseRow(ws, id);
            if (row < 2) return Task.FromResult<Exercise?>(null);

            var ex = ReadExercise(ws, row, userId)!;
            if (data.Name        is not null) ex.Name        = data.Name.Trim();
            if (data.MuscleGroup is not null) ex.MuscleGroup = data.MuscleGroup;
            if (data.Muscles     is not null) ex.Muscles     = data.Muscles;
            if (data.Description is not null) ex.Description = data.Description;
            if (data.Type        is not null) ex.Type        = data.Type;
            if (data.Params      is not null) ex.Params      = data.Params;
            ex.UpdatedAt = NowIso();

            WriteExerciseRow(ws, row, ex, sheetName ?? SafeSheetName(ex.Name));
            wb.Save();
            return Task.FromResult<Exercise?>(ex);
        }
    }

    public Task<bool> DeleteExerciseAsync(string id, string userId)
    {
        lock (_lock)
        {
            EnsureUserFile(userId);
            using var wb = new XLWorkbook(UserFilePath(userId));
            var ws = wb.Worksheet("_Exercises");
            var (row, sheetName) = FindExerciseRow(ws, id);
            if (row < 2) return Task.FromResult(false);

            if (sheetName is not null && wb.TryGetWorksheet(sheetName, out var setSheet))
                setSheet.Delete();

            ws.Row(row).Delete();
            wb.Save();
            return Task.FromResult(true);
        }
    }

    // ── IDatabase: Sessions ───────────────────────────────────────────────────

    public Task<List<WorkoutSession>> GetSessionsAsync(string userId)
    {
        lock (_lock)
        {
            EnsureUserFile(userId);
            using var wb = new XLWorkbook(UserFilePath(userId));
            var sessions = LoadAllSessions(wb, userId);
            return Task.FromResult(sessions.OrderByDescending(s => s.Date).ToList());
        }
    }

    public Task<WorkoutSession?> GetSessionByDateAsync(string userId, string date)
    {
        lock (_lock)
        {
            EnsureUserFile(userId);
            using var wb = new XLWorkbook(UserFilePath(userId));
            return Task.FromResult(FindSessionByDate(wb, userId, date));
        }
    }

    public Task<WorkoutSession> GetOrCreateSessionByDateAsync(string userId, string date)
    {
        lock (_lock)
        {
            EnsureUserFile(userId);
            using var wb = new XLWorkbook(UserFilePath(userId));
            var existing = FindSessionByDate(wb, userId, date);
            if (existing is not null) return Task.FromResult(existing);

            var session = new WorkoutSession
            {
                Id        = NewId(),
                UserId    = userId,
                Date      = date,
                Sets      = [],
                CreatedAt = NowIso(),
            };
            AppendRow(wb.Worksheet("_Sessions"), session.Id, session.Date, session.CreatedAt);
            wb.Save();
            return Task.FromResult(session);
        }
    }

    public Task<WorkoutSet> AddSetAsync(string sessionId, string userId, CreateSetRequest data)
    {
        lock (_lock)
        {
            EnsureUserFile(userId);
            using var wb = new XLWorkbook(UserFilePath(userId));

            // verify session exists and get its date
            var sessWs = wb.Worksheet("_Sessions");
            int sessRow = FindSessionRowById(sessWs, sessionId);
            if (sessRow < 2) throw new InvalidOperationException("Session not found");
            var sessionDate = CellStr(sessWs.Cell(sessRow, SC.Date)) ?? TodayIso();

            // find exercise sheet
            var exWs = wb.Worksheet("_Exercises");
            var (_, sheetName) = FindExerciseRow(exWs, data.ExerciseId);
            if (sheetName is null) throw new InvalidOperationException("Exercise not found");
            if (!wb.TryGetWorksheet(sheetName, out var setSheet))
                throw new InvalidOperationException($"Sheet '{sheetName}' not found");

            var set = new WorkoutSet
            {
                Id              = NewId(),
                ExerciseId      = data.ExerciseId,
                Timestamp       = data.Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Weight          = data.Weight,
                Reps            = data.Reps,
                Tut             = data.Tut,
                Rpe             = data.Rpe,
                Duration        = data.Duration,
                Pace            = data.Pace,
                Distance        = data.Distance,
                Incline         = data.Incline,
                HeartRate       = data.HeartRate,
                Side            = data.Side,
                BreathingCycles = data.BreathingCycles,
            };

            AppendRow(setSheet,
                set.Id, sessionId, sessionDate, set.Timestamp,
                set.Weight, set.Reps, set.Tut, set.Rpe,
                set.Duration, set.Pace, set.Distance, set.Incline,
                set.HeartRate, set.Side, set.BreathingCycles);

            wb.Save();
            return Task.FromResult(set);
        }
    }

    public Task<bool> RemoveSetAsync(string setId, string userId)
    {
        lock (_lock)
        {
            EnsureUserFile(userId);
            using var wb = new XLWorkbook(UserFilePath(userId));
            var exWs = wb.Worksheet("_Exercises");
            for (int r = 2; r <= LastDataRow(exWs); r++)
            {
                var sheetName = CellStr(exWs.Cell(r, EC.SheetName));
                if (sheetName is null || !wb.TryGetWorksheet(sheetName, out var setSheet)) continue;

                for (int sr = 2; sr <= LastDataRow(setSheet); sr++)
                {
                    if (CellStr(setSheet.Cell(sr, SetC.SetId)) != setId) continue;
                    setSheet.Row(sr).Delete();
                    wb.Save();
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }
    }

    public Task<List<WorkoutSession>> GetHistoryForExerciseAsync(string userId, string exerciseId, int limit)
    {
        var today = TodayIso();
        lock (_lock)
        {
            EnsureUserFile(userId);
            using var wb = new XLWorkbook(UserFilePath(userId));

            var exWs = wb.Worksheet("_Exercises");
            var (_, sheetName) = FindExerciseRow(exWs, exerciseId);
            if (sheetName is null || !wb.TryGetWorksheet(sheetName, out var setSheet))
                return Task.FromResult(new List<WorkoutSession>());

            var sessionMap = LoadSessionMap(wb.Worksheet("_Sessions"), userId);

            // group sets by session
            var bySession = new Dictionary<string, List<WorkoutSet>>();
            for (int r = 2; r <= LastDataRow(setSheet); r++)
            {
                var sessId = CellStr(setSheet.Cell(r, SetC.SessionId)) ?? "";
                if (!bySession.ContainsKey(sessId)) bySession[sessId] = [];
                bySession[sessId].Add(ReadSet(setSheet, r, exerciseId));
            }

            var result = new List<WorkoutSession>();
            foreach (var (sessId, sets) in bySession)
            {
                if (!sessionMap.TryGetValue(sessId, out var sess) || sess.Date == today) continue;
                result.Add(new WorkoutSession
                {
                    Id        = sess.Id,
                    UserId    = userId,
                    Date      = sess.Date,
                    CreatedAt = sess.CreatedAt,
                    Sets      = sets,
                });
            }

            return Task.FromResult(result.OrderByDescending(s => s.Date).Take(limit).ToList());
        }
    }

    // ── File init ─────────────────────────────────────────────────────────────

    private void EnsureUsersFile()
    {
        if (File.Exists(UsersFilePath)) return;
        var wb = new XLWorkbook();
        AddHeaderSheet(wb, "Users", UserHeaders);
        wb.SaveAs(UsersFilePath);
    }

    private void EnsureUserFile(string userId)
    {
        var path = UserFilePath(userId);
        if (File.Exists(path)) return;
        var wb = new XLWorkbook();
        AddHeaderSheet(wb, "_Exercises", ExerciseHeaders);
        AddHeaderSheet(wb, "_Sessions",  SessionHeaders);
        wb.SaveAs(path);
    }

    // ── Workbook helpers ──────────────────────────────────────────────────────

    private static IXLWorksheet AddHeaderSheet(XLWorkbook wb, string name, string[] headers)
    {
        var ws = wb.Worksheets.Add(name);
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];
        var headerRow = ws.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E1E2E");
        headerRow.Style.Font.FontColor = XLColor.White;
        ws.SheetView.FreezeRows(1);
        return ws;
    }

    private static void AppendRow(IXLWorksheet ws, params object?[] values)
    {
        int row = Math.Max(LastDataRow(ws) + 1, 2);
        for (int i = 0; i < values.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = values[i] switch
            {
                null        => Blank.Value,
                string s    => (XLCellValue)s,
                int n       => (XLCellValue)n,
                double d    => (XLCellValue)d,
                long l      => (XLCellValue)(double)l,
                bool b      => (XLCellValue)b,
                _           => (XLCellValue)(values[i]?.ToString() ?? string.Empty),
            };
        }
    }

    // ── Sheet name helpers ────────────────────────────────────────────────────

    private static string SafeSheetName(string name)
    {
        var invalid = new[] { '\\', '/', '*', '[', ']', ':', '?', '\'' };
        var safe = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return safe.Length > 31 ? safe[..31] : (safe.Length > 0 ? safe : "Exercise");
    }

    private static string UniqueSheetName(XLWorkbook wb, string baseName)
    {
        if (!wb.TryGetWorksheet(baseName, out _)) return baseName;
        for (int i = 2; i < 1000; i++)
        {
            var suffix    = $"_{i}";
            var candidate = baseName.Length + suffix.Length > 31
                ? baseName[..(31 - suffix.Length)] + suffix
                : baseName + suffix;
            if (!wb.TryGetWorksheet(candidate, out _)) return candidate;
        }
        return Guid.NewGuid().ToString()[..8];
    }

    // ── Cell readers ──────────────────────────────────────────────────────────

    private static string? CellStr(IXLCell c)
    {
        if (c.IsEmpty()) return null;
        var v = c.Value;
        if (v.IsBlank) return null;
        return v.ToString();
    }

    private static double? CellDouble(IXLCell c)
        => c.TryGetValue<double>(out var v) ? v : null;

    private static int? CellInt(IXLCell c)
        => c.TryGetValue<int>(out var v) ? v : null;

    private static long CellLong(IXLCell c)
        => c.TryGetValue<long>(out var v) ? v : 0L;

    // ── Exercise readers / writers ────────────────────────────────────────────

    private static Exercise? ReadExercise(IXLWorksheet ws, int row, string userId)
    {
        var id = CellStr(ws.Cell(row, EC.Id));
        if (string.IsNullOrEmpty(id)) return null;

        var musclesJson = CellStr(ws.Cell(row, EC.Muscles)) ?? "[]";
        var paramsJson  = CellStr(ws.Cell(row, EC.ParamsJson)) ?? "{}";

        return new Exercise
        {
            Id          = id,
            UserId      = userId,
            Name        = CellStr(ws.Cell(row, EC.Name))        ?? "",
            MuscleGroup = CellStr(ws.Cell(row, EC.MuscleGroup)) ?? "",
            Muscles     = JsonSerializer.Deserialize<List<string>>(musclesJson, _jsonOpts) ?? [],
            Description = CellStr(ws.Cell(row, EC.Description)) ?? "",
            Type        = CellStr(ws.Cell(row, EC.Type))        ?? "",
            Params      = JsonSerializer.Deserialize<ExerciseParams>(paramsJson, _jsonOpts) ?? new(),
            CreatedAt   = CellStr(ws.Cell(row, EC.CreatedAt))   ?? "",
            UpdatedAt   = CellStr(ws.Cell(row, EC.UpdatedAt))   ?? "",
        };
    }

    private static void AppendExerciseRow(IXLWorksheet ws, Exercise ex, string sheetName)
        => AppendRow(ws,
            ex.Id, ex.Name, ex.MuscleGroup,
            JsonSerializer.Serialize(ex.Muscles, _jsonOpts),
            ex.Description, ex.Type,
            JsonSerializer.Serialize(ex.Params, _jsonOpts),
            ex.CreatedAt, ex.UpdatedAt, sheetName);

    private static void WriteExerciseRow(IXLWorksheet ws, int row, Exercise ex, string sheetName)
    {
        ws.Cell(row, EC.Id).Value          = ex.Id;
        ws.Cell(row, EC.Name).Value        = ex.Name;
        ws.Cell(row, EC.MuscleGroup).Value = ex.MuscleGroup;
        ws.Cell(row, EC.Muscles).Value     = JsonSerializer.Serialize(ex.Muscles, _jsonOpts);
        ws.Cell(row, EC.Description).Value = ex.Description;
        ws.Cell(row, EC.Type).Value        = ex.Type;
        ws.Cell(row, EC.ParamsJson).Value  = JsonSerializer.Serialize(ex.Params, _jsonOpts);
        ws.Cell(row, EC.CreatedAt).Value   = ex.CreatedAt;
        ws.Cell(row, EC.UpdatedAt).Value   = ex.UpdatedAt;
        ws.Cell(row, EC.SheetName).Value   = sheetName;
    }

    private static void WriteSetSheetHeader(IXLWorksheet ws)
    {
        for (int i = 0; i < SetHeaders.Length; i++)
            ws.Cell(1, i + 1).Value = SetHeaders[i];
        var h = ws.Row(1);
        h.Style.Font.Bold = true;
        h.Style.Fill.BackgroundColor = XLColor.FromHtml("#313244");
        h.Style.Font.FontColor = XLColor.White;
        ws.SheetView.FreezeRows(1);
    }

    // ── Exercise row finder ───────────────────────────────────────────────────

    private static (int row, string? sheetName) FindExerciseRow(IXLWorksheet ws, string exerciseId)
    {
        for (int r = 2; r <= LastDataRow(ws); r++)
            if (CellStr(ws.Cell(r, EC.Id)) == exerciseId)
                return (r, CellStr(ws.Cell(r, EC.SheetName)));
        return (0, null);
    }

    // ── Session helpers ───────────────────────────────────────────────────────

    private static int FindSessionRowById(IXLWorksheet ws, string sessionId)
    {
        for (int r = 2; r <= LastDataRow(ws); r++)
            if (CellStr(ws.Cell(r, SC.Id)) == sessionId) return r;
        return 0;
    }

    private WorkoutSession? FindSessionByDate(XLWorkbook wb, string userId, string date)
    {
        var ws = wb.Worksheet("_Sessions");
        for (int r = 2; r <= LastDataRow(ws); r++)
        {
            if (CellStr(ws.Cell(r, SC.Date)) != date) continue;
            var sessionId = CellStr(ws.Cell(r, SC.Id)) ?? "";
            return new WorkoutSession
            {
                Id        = sessionId,
                UserId    = userId,
                Date      = date,
                CreatedAt = CellStr(ws.Cell(r, SC.CreatedAt)) ?? "",
                Sets      = LoadSetsForSession(wb, sessionId),
            };
        }
        return null;
    }

    private Dictionary<string, WorkoutSession> LoadSessionMap(IXLWorksheet ws, string userId)
    {
        var map = new Dictionary<string, WorkoutSession>();
        for (int r = 2; r <= LastDataRow(ws); r++)
        {
            var id = CellStr(ws.Cell(r, SC.Id)) ?? "";
            if (string.IsNullOrEmpty(id)) continue;
            map[id] = new WorkoutSession
            {
                Id        = id,
                UserId    = userId,
                Date      = CellStr(ws.Cell(r, SC.Date))      ?? "",
                CreatedAt = CellStr(ws.Cell(r, SC.CreatedAt)) ?? "",
                Sets      = [],
            };
        }
        return map;
    }

    private List<WorkoutSession> LoadAllSessions(XLWorkbook wb, string userId)
    {
        var sessionMap = LoadSessionMap(wb.Worksheet("_Sessions"), userId);
        var bySession  = new Dictionary<string, List<WorkoutSet>>();

        var exWs = wb.Worksheet("_Exercises");
        for (int r = 2; r <= LastDataRow(exWs); r++)
        {
            var exerciseId = CellStr(exWs.Cell(r, EC.Id)) ?? "";
            var sheetName  = CellStr(exWs.Cell(r, EC.SheetName));
            if (sheetName is null || !wb.TryGetWorksheet(sheetName, out var setSheet)) continue;

            for (int sr = 2; sr <= LastDataRow(setSheet); sr++)
            {
                var sessId = CellStr(setSheet.Cell(sr, SetC.SessionId)) ?? "";
                if (!bySession.ContainsKey(sessId)) bySession[sessId] = [];
                bySession[sessId].Add(ReadSet(setSheet, sr, exerciseId));
            }
        }

        return sessionMap.Values.Select(s =>
        {
            s.Sets = bySession.TryGetValue(s.Id, out var sets) ? sets : [];
            return s;
        }).ToList();
    }

    private List<WorkoutSet> LoadSetsForSession(XLWorkbook wb, string sessionId)
    {
        var sets = new List<WorkoutSet>();
        var exWs = wb.Worksheet("_Exercises");
        for (int r = 2; r <= LastDataRow(exWs); r++)
        {
            var exerciseId = CellStr(exWs.Cell(r, EC.Id)) ?? "";
            var sheetName  = CellStr(exWs.Cell(r, EC.SheetName));
            if (sheetName is null || !wb.TryGetWorksheet(sheetName, out var setSheet)) continue;

            for (int sr = 2; sr <= LastDataRow(setSheet); sr++)
                if (CellStr(setSheet.Cell(sr, SetC.SessionId)) == sessionId)
                    sets.Add(ReadSet(setSheet, sr, exerciseId));
        }
        return sets;
    }

    private static WorkoutSet ReadSet(IXLWorksheet ws, int row, string exerciseId) => new()
    {
        Id              = CellStr(ws.Cell(row, SetC.SetId))           ?? "",
        ExerciseId      = exerciseId,
        Timestamp       = CellLong(ws.Cell(row, SetC.Timestamp)),
        Weight          = CellDouble(ws.Cell(row, SetC.Weight)),
        Reps            = CellInt(ws.Cell(row, SetC.Reps)),
        Tut             = CellInt(ws.Cell(row, SetC.Tut)),
        Rpe             = CellDouble(ws.Cell(row, SetC.Rpe)),
        Duration        = CellDouble(ws.Cell(row, SetC.Duration)),
        Pace            = CellDouble(ws.Cell(row, SetC.Pace)),
        Distance        = CellDouble(ws.Cell(row, SetC.Distance)),
        Incline         = CellDouble(ws.Cell(row, SetC.Incline)),
        HeartRate       = CellInt(ws.Cell(row, SetC.HeartRate)),
        Side            = CellStr(ws.Cell(row, SetC.Side)),
        BreathingCycles = CellInt(ws.Cell(row, SetC.BreathingCycles)),
    };
}
