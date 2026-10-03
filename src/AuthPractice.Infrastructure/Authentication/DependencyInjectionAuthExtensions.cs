using AuthPractice.Core.Auth;
using AuthPractice.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPractice.Infrastructure.Authentication;

public static class DependencyInjectionAuthExtensions
{
    public static IServiceCollection AddTestAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services
            // Регистрирует сервисы аутентификации; DefaultScheme — схема, которую
            // UseAuthentication вызывает для каждого запроса
            .AddAuthentication(TestAuthenticationDefaults.SchemeName)
            // Связываем: имя схемы + тип Options + тип Handler.
            // Лямбда — настройка Options (здесь биндим из appsettings.json)
            .AddScheme<TestAuthenticationOptions, TestAuthenticationHandler>(
                TestAuthenticationDefaults.SchemeName,
                options => configuration.GetSection(TestAuthenticationDefaults.ConfigSection).Bind(options));

        // Политики: правила "кому можно" поверх уже аутентифицированного пользователя
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.AdminOnly, policy => policy.RequireRole(UserRoles.Admin));

        return services;
    }
}
