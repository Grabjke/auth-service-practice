using Core.Abstractions;
using Framework.Endpoints;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPractice.Core;

public static class DependencyInjectionCoreExtensions
{
    // Регистрация всего слоя Core
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjectionCoreExtensions).Assembly;

        services
            .AddEndpoints(assembly)  // все IEndpoint из сборки (Framework)
            .AddHandlers(assembly);  // все IQueryHandler/ICommandHandler (Core, через Scrutor)

        return services;
    }
}
