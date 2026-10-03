using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPractice.Infrastructure;

public static class DependencyInjectionInfrastructureExtensions
{
    // Точка регистрации инфраструктуры (сюда в ветке практики добавится аутентификация)
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
