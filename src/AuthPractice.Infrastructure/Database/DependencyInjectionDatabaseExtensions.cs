using AuthPractice.Core.Auth.RefreshTokens;
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

        services.AddDbContext<AppIdentityDbContext>(options => options
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AppIdentityDbContext.Schema))
            // Все имена в БД — snake_case: колонки, PK/FK, индексы (имена таблиц заданы явно в OnModelCreating)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddScoped<IRefreshSessionRepository, RefreshSessionRepository>();

        return services;
    }

    // Накатывает миграции AppIdentityDbContext при старте (в Docker — сразу после healthcheck Postgres)
    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
        await RenameLegacyMigrationsHistoryColumnsAsync(dbContext);
        await dbContext.Database.MigrateAsync();
    }

    // UseSnakeCaseNamingConvention переименовывает и колонки таблицы истории миграций
    // (MigrationId → migration_id). Базы, созданные до перехода на snake_case, иначе не стартуют:
    // EF не найдёт migration_id. Идемпотентно — на новой БД и на уже переведённой ничего не делает.
    // Базы ещё нет (её создаст MigrateAsync) — переименовывать нечего
    private static async Task RenameLegacyMigrationsHistoryColumnsAsync(AppIdentityDbContext dbContext)
    {
        if (!await dbContext.Database.CanConnectAsync())
            return;

        await dbContext.Database.ExecuteSqlRawAsync($$"""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = '{{AppIdentityDbContext.Schema}}'
                      AND table_name = '__EFMigrationsHistory'
                      AND column_name = 'MigrationId')
                THEN
                    ALTER TABLE "{{AppIdentityDbContext.Schema}}"."__EFMigrationsHistory" RENAME COLUMN "MigrationId" TO migration_id;
                    ALTER TABLE "{{AppIdentityDbContext.Schema}}"."__EFMigrationsHistory" RENAME COLUMN "ProductVersion" TO product_version;
                END IF;
            END $$;
            """);
    }
}
