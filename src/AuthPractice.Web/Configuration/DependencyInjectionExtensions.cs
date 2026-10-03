using AuthPractice.Core;
using AuthPractice.Infrastructure;

namespace AuthPractice.Web.Configuration;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi(); // генерация /openapi/v1.json

        services
            .AddInfrastructure(configuration)
            .AddCore();

        return services;
    }
}
