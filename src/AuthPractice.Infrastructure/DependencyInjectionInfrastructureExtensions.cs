using AuthPractice.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPractice.Infrastructure;

public static class DependencyInjectionInfrastructureExtensions
{
    // Точка регистрации инфраструктуры
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTestAuthentication(configuration);

        return services;
    }
}
