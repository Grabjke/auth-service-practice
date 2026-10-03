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

        // Все endpoints из Core вешаются на префикс /api
        var apiGroup = app.MapGroup("/api");
        app.MapEndpoints(apiGroup);

        return app;
    }
}
