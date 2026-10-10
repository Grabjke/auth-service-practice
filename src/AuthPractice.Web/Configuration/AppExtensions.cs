using AuthPractice.Infrastructure.Authentication;
using Framework.Endpoints;
using Framework.Middlewares;

namespace AuthPractice.Web.Configuration;

public static class AppExtensions
{
    public static WebApplication Configure(this WebApplication app)
    {
        // Первым — ловит исключения из всего, что ниже, и отдаёт Envelope с ошибкой
        app.UseExceptionMiddleware();

        app.MapOpenApi();
        app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Auth Practice V1"));

        // Порядок важен: сначала "кто ты" (аутентификация), потом "можно ли" (авторизация)
        app.UseAuthentication(); // → CookieAuthenticationHandler: cookie → HttpContext.User
        app.UseUserScopeData();  // → cookie или Bearer → scoped UserScopeData (для хендлеров через DI)
        app.UseAuthorization();  // → проверка RequireAuthorization / политик, 401/403

        // Все endpoints из Core вешаются на префикс /api
        var apiGroup = app.MapGroup("/api");
        app.MapEndpoints(apiGroup);

        return app;
    }
}
