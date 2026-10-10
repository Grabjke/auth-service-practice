using AuthPractice.Core.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPractice.Infrastructure.Database;

public static class DependencyInjectionDatabaseExtensions
{
    public const string ConnectionStringName = "IdentityDb";

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"ConnectionStrings:{ConnectionStringName} is not set");

        services.AddDbContext<AppIdentityDbContext>(options => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AppIdentityDbContext.Schema)));

        services.AddScoped<ITransactionManager, TransactionManager>();

        return services;
    }

    // Накатывает миграции AppIdentityDbContext при старте (в Docker — сразу после healthcheck Postgres)
    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
