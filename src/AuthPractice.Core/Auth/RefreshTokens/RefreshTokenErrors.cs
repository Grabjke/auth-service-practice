using SharedKernel;

namespace AuthPractice.Core.Auth.RefreshTokens;

// Ошибки refresh-сессий (POST /api/auth/jwt/refresh)
public static class RefreshTokenErrors
{
    // 401: сессии нет / истекла / отозвана — клиенту только заново логиниться
    public static readonly Error InvalidToken =
        Error.Authentication("refresh.invalid", "Сессия недействительна, войдите заново");

    // 409: токен только что ротирован параллельным запросом (другая вкладка) — повторите refresh,
    // браузер уже пришлёт новую cookie. Семья при этом НЕ отзывается
    public static readonly Error ConcurrentRefresh =
        Error.Conflict("refresh.concurrent", "Сессия только что обновлена параллельным запросом, повторите");
}
