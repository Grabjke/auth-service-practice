namespace AuthPractice.Core.Auth;

// Имена схем аутентификации. Регистрируются в Infrastructure,
// endpoints выбирают их через RequireAuthorization / AuthenticationSchemes
public static class AuthSchemes
{
    // Default-схема (веб-фронт): = CookieAuthenticationDefaults.AuthenticationScheme
    public const string Cookie = "Cookies";

    // Именованная вторая схема (mobile / сервисы): = JwtBearerDefaults.AuthenticationScheme
    public const string Bearer = "Bearer";

    // Для endpoint'ов, которые пускают обоих клиентов
    public const string CookieOrBearer = $"{Cookie},{Bearer}";
}
