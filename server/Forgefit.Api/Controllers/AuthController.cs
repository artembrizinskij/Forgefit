using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Forgefit.Api.Database;
using Forgefit.Api.Infrastructure;
using Forgefit.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Forgefit.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IDatabase db, JwtSettings jwt) : ControllerBase
{
    // ── POST /api/auth/register ───────────────────────────────────────────────

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            throw new HttpException(400, "Email обязателен");
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            throw new HttpException(400, "Пароль должен содержать минимум 6 символов");
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new HttpException(400, "Имя обязательно");

        var existing = await db.FindUserByEmailAsync(req.Email);
        if (existing is not null)
            throw new HttpException(409, "Пользователь с таким email уже существует");

        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        var user = await db.CreateUserAsync(req.Email, hash, req.Name);

        return StatusCode(201, new AuthResponse(GenerateToken(user), ToPublic(user)));
    }

    // ── POST /api/auth/login ──────────────────────────────────────────────────

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            throw new HttpException(400, "Email и пароль обязательны");

        var user = await db.FindUserByEmailAsync(req.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new HttpException(401, "Неверный email или пароль");

        return Ok(new AuthResponse(GenerateToken(user), ToPublic(user)));
    }

    // ── GET /api/auth/me ──────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = GetUserId();
        var user = await db.FindUserByIdAsync(userId)
            ?? throw new HttpException(401, "Пользователь не найден");

        return Ok(new { user = ToPublic(user) });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string GetUserId() =>
        User.FindFirstValue("userId")
        ?? throw new HttpException(401, "Требуется авторизация");

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: [new Claim("userId", user.Id)],
            expires: DateTime.UtcNow.Add(jwt.Expiry),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static PublicUser ToPublic(User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        Name = u.Name,
        CreatedAt = u.CreatedAt,
    };
}
