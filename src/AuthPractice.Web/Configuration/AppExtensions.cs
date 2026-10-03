using AuthPractice.Core.Abstractions;

namespace AuthPractice.Web.Configuration;

public static class AppExtensions
{
    public static WebApplication Configure(this WebApplication app)
    {
        app.MapOpenApi();
        app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Auth Practice V1"));

        // Порядок важен: сначала "кто ты" (аутентификация), потом "можно ли" (авторизация)
        app.UseAuthentication(); // → TestAuthenticationHandler.HandleAuthenticateAsync
        app.UseAuthorization();  // → проверка RequireAuthorization / политик, 401/403

        // Все endpoints из Core вешаются на префикс /api
        var apiGroup = app.MapGroup("/api");
        app.MapEndpoints(apiGroup);

        return app;
    }
}
