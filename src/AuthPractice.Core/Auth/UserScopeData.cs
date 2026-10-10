using System.Security.Authentication;
using System.Security.Claims;

namespace AuthPractice.Core.Auth;

// Текущий пользователь запроса. Scoped: один экземпляр на HTTP-запрос.
// Заполняет UserScopeDataMiddleware (после UseAuthentication) — из cookie ИЛИ Bearer-токена,
// хендлеры получают через DI вместо разбора HttpContext.User / IHttpContextAccessor
public sealed class UserScopeData
{
    public bool IsAuthenticated { get; private set; }

    public Guid? UserId { get; private set; }

    public string? UserName { get; private set; }

    public string? Email { get; private set; }

    public IReadOnlyList<string> Roles { get; private set; } = [];

    public string? SecurityStamp { get; private set; }

    // Какой схемой аутентифицирован: AuthSchemes.Cookie / AuthSchemes.Bearer
    public string? AuthenticationScheme { get; private set; }

    // Для хендлеров за RequireAuthorization: пользователь там обязан быть.
    // AuthenticationException → ExceptionMiddleware отдаст 401
    public Guid GetRequiredUserId() =>
        UserId ?? throw new AuthenticationException("Пользователь не аутентифицирован");

    public bool IsInRole(string role) => Roles.Contains(role);

    // Вызывается только из middleware. Повторное заполнение в рамках запроса — ошибка
    public void Fill(ClaimsPrincipal principal, string authenticationScheme)
    {
        if (IsAuthenticated)
            throw new InvalidOperationException("UserScopeData уже заполнен для этого запроса");

        UserId = Guid.TryParse(principal.GetUserId(), out var id) ? id : null;
        UserName = principal.GetUserName();
        Email = principal.GetEmail();
        Roles = principal.GetRoles();
        SecurityStamp = principal.GetSecurityStamp();
        AuthenticationScheme = authenticationScheme;
        IsAuthenticated = UserId is not null;
    }
}
