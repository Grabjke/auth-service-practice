using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AuthPractice.Core.Abstractions;

public static class EndpointExtensions
{
    // Находит все классы IEndpoint в сборке и кладёт их в DI
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var descriptors = assembly.DefinedTypes
            .Where(t => t is { IsAbstract: false, IsInterface: false } && t.IsAssignableTo(typeof(IEndpoint)))
            .Select(t => ServiceDescriptor.Transient(typeof(IEndpoint), t));

        services.TryAddEnumerable(descriptors);
        return services;
    }

    // Достаёт все IEndpoint из DI и вызывает MapEndpoint на группе маршрутов (/api)
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app, RouteGroupBuilder group)
    {
        foreach (var endpoint in app.ServiceProvider.GetRequiredService<IEnumerable<IEndpoint>>())
            endpoint.MapEndpoint(group);

        return app;
    }
}
