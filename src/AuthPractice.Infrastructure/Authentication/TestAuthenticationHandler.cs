using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthPractice.Infrastructure.Authentication;

// Путь запроса (ставь брейкпоинты в этом порядке):
//  1. app.UseAuthentication()  → HandleAuthenticateAsync  — кто ты? (заполняет HttpContext.User)
//  2. app.UseAuthorization()   → проверка RequireAuthorization / политики
//       - не аутентифицирован  → HandleChallengeAsync  (401)
//       - нет прав (роль)       → HandleForbiddenAsync  (403)
//  3. всё ок                   → endpoint (GetMeEndpoint и т.д.)
//
// Handler создаётся НА КАЖДЫЙ запрос (transient), Options приходят из IOptionsMonitor по имени схемы.
public sealed class TestAuthenticationHandler(
    IOptionsMonitor<TestAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<TestAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Options — уже забинженные TestAuthenticationOptions для нашей схемы
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var token) || string.IsNullOrEmpty(token))
        {
            // NoResult = "меня это не касается": пользователь остаётся анонимным
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Options.Users.TryGetValue(token.ToString(), out var user))
        {
            // Fail = токен есть, но он неверный (тоже приведёт к 401 на защищённом endpoint)
            return Task.FromResult(AuthenticateResult.Fail("Invalid test token"));
        }

        // Claims — "факты" о пользователе
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, token.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
        };

        // Identity (с именем схемы => IsAuthenticated = true) → Principal → Ticket
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        // Success → ASP.NET положит principal в HttpContext.User
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    // 401: вызывается авторизацией, если пользователь не аутентифицирован
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers.WWWAuthenticate = $"{Scheme.Name} header=\"{Options.HeaderName}\"";
        return Task.CompletedTask;
    }

    // 403: пользователь известен, но политика не пропустила (например, нет роли admin)
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        return Task.CompletedTask;
    }
}
