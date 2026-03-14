namespace Forgefit.Api.Infrastructure;

/// <summary>JWT configuration injected into controllers via DI.</summary>
public sealed record JwtSettings(string Secret, string ExpiresIn)
{
    /// <summary>Parses "7d", "24h", "30m" into a TimeSpan.</summary>
    public TimeSpan Expiry => ExpiresIn switch
    {
        var s when s.EndsWith('d') && int.TryParse(s[..^1], out var d) => TimeSpan.FromDays(d),
        var s when s.EndsWith('h') && int.TryParse(s[..^1], out var h) => TimeSpan.FromHours(h),
        var s when s.EndsWith('m') && int.TryParse(s[..^1], out var m) => TimeSpan.FromMinutes(m),
        _ => TimeSpan.FromDays(7),
    };
}
