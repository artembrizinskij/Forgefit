using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Forgefit.Api.Database;
using Forgefit.Api.Endpoints;
using Forgefit.Api.Infrastructure;
using Forgefit.Api.Middleware;
using Forgefit.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ─────────────────────────────────────────────────────────────
// Environment variables override appsettings.json values.
// Docker / production sets: JWT_SECRET, JWT_EXPIRES_IN, CORS_ORIGIN, PORT

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["Jwt:Secret"]
    ?? "dev-secret-change-in-production!x1"; // min 32 chars (256 bit) for HS256

var jwtExpiresIn = Environment.GetEnvironmentVariable("JWT_EXPIRES_IN")
    ?? builder.Configuration["Jwt:ExpiresIn"]
    ?? "7d";

var corsOrigins = (
    Environment.GetEnvironmentVariable("CORS_ORIGIN")
    ?? builder.Configuration["Cors:Origins"]
    ?? "http://localhost:5173"
).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

// ── Settings ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton(new JwtSettings(jwtSecret, jwtExpiresIn));

// ── JSON ──────────────────────────────────────────────────────────────────────
builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    opts.SerializerOptions.PropertyNameCaseInsensitive = true;
});

// ── JWT Authentication ────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.MapInboundClaims = false; // Keep custom claim names (e.g. "userId") as-is
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero,
        };

        // Return JSON error on 401 instead of redirect
        opts.Events = new JwtBearerEvents
        {
            OnChallenge = ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/problem+json";
                return ctx.Response.WriteAsync(
                    """{"type":"https://httpstatuses.com/401","title":"Unauthorized","status":401,"detail":"Требуется авторизация"}""");
            },
        };
    });

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
    )
);

// ── Database ───────────────────────────────────────────────────────────────────
// DB_TYPE=memory  → volatile in-process store (default for tests)
// DB_TYPE=xlsx    → Excel files in DB_DATA_DIR (default, persistent)
//
// To add a real DB: implement IDatabase and register it below.
var dbType = (Environment.GetEnvironmentVariable("DB_TYPE")
    ?? builder.Configuration["Database:Type"]
    ?? "xlsx").ToLowerInvariant();

var dataDir = Environment.GetEnvironmentVariable("DB_DATA_DIR")
    ?? builder.Configuration["Database:DataDir"]
    ?? "data";

if (dbType == "memory")
    builder.Services.AddSingleton<IDatabase, InMemoryDatabase>();
else
    builder.Services.AddSingleton<IDatabase>(_ => new XlsxDatabase(dataDir));

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<ExerciseService>();
builder.Services.AddSingleton<WorkoutService>();

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Forgefit API", Version = "v1" });

    // JWT bearer auth in Swagger UI
    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme,
        },
    };

    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, bearerScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { bearerScheme, Array.Empty<string>() },
    });
});

// ── App pipeline ──────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ─────────────────────────────────────────────────────────────────
app.MapGet("/api/health", () => TypedResults.Ok(new
{
    status = "ok",
    timestamp = DateTime.UtcNow.ToString("O"),
})).WithTags("Health").WithSummary("Health check");

app.MapAuthEndpoints();
app.MapExerciseEndpoints();
app.MapWorkoutEndpoints();

// URL binding priority:
//   1. ASPNETCORE_URLS env var  (set in Docker: http://0.0.0.0:3000)
//   2. launchSettings.json      (local dev:     http://localhost:5000)
app.Run();
