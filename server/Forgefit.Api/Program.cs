using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Forgefit.Api.Database;
using Forgefit.Api.Infrastructure;
using Forgefit.Api.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ────────────────────────────────────────────────────────────
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

// Expose settings to controllers via DI
builder.Services.AddSingleton(new JwtSettings(jwtSecret, jwtExpiresIn));

// ── Controllers & JSON ────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
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
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync("{\"error\":\"Требуется авторизация\"}");
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

// ── Database (swap implementation here for a real DB) ─────────────────────────
// To use a different database, replace InMemoryDatabase with your implementation:
//   builder.Services.AddSingleton<IDatabase, PostgresDatabase>();
//   builder.Services.AddSingleton<IDatabase, MongoDatabase>();
builder.Services.AddSingleton<IDatabase, InMemoryDatabase>();

// ── App pipeline ──────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Health check
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    timestamp = DateTime.UtcNow.ToString("O"),
}));

app.MapControllers();

// URL binding priority:
//   1. ASPNETCORE_URLS env var  (set in Docker: http://0.0.0.0:3000)
//   2. launchSettings.json      (local dev:     http://localhost:5000)
app.Run();
