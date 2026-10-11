using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AuthPractice.Core.Auth.RefreshTokens;

// Refresh живёт только в HttpOnly-cookie: никогда в JSON-ответе, теле запроса или localStorage
public sealed class RefreshTokenCookie(IHttpContextAccessor httpContextAccessor, IOptions<RefreshTokenOptions> options)
{
    private readonly RefreshTokenOptions _options = options.Value;

    private HttpContext HttpContext => httpContextAccessor.HttpContext!;

    public string? Read() =>
        HttpContext.Request.Cookies.TryGetValue(_options.CookieName, out var value) && !string.IsNullOrEmpty(value)
            ? value
            : null;

    public void Write(string rawToken, DateTimeOffset expiresAt) =>
        HttpContext.Response.Cookies.Append(_options.CookieName, rawToken, CreateCookieOptions(expiresAt));

    // Удалить можно только с теми же Path/SameSite/Secure, с которыми ставили
    public void Delete() =>
        HttpContext.Response.Cookies.Delete(_options.CookieName, CreateCookieOptions(expiresAt: null));

    private CookieOptions CreateCookieOptions(DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,                 // JS (и XSS) cookie не видит
        Secure = true,                   // только HTTPS; браузеры делают исключение для http://localhost
        SameSite = SameSiteMode.Strict,  // фронт и API на одном origin (прокси) — cross-site не нужен
        Path = _options.CookiePath,
        Expires = expiresAt,
        IsEssential = true,
    };
}
