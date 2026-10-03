using AuthPractice.Core.Abstractions;
using AuthPractice.Core.Features.Ping;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPractice.Core;

public static class DependencyInjectionCoreExtensions
{
    // Регистрация всего слоя Core: endpoints + handlers
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddEndpoints(typeof(DependencyInjectionCoreExtensions).Assembly);

        services.AddScoped<PingHandler>();

        return services;
    }
}
