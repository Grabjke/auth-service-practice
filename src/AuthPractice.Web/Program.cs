using AuthPractice.Infrastructure.Database;
using AuthPractice.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 1. Регистрируем сервисы всех слоёв в DI
builder.Services.AddConfiguration(builder.Configuration);

var app = builder.Build();

// 2. Накатываем миграции AppIdentityDbContext в Postgres
await app.Services.ApplyMigrationsAsync();

// 3. Настраиваем pipeline (middleware + маршруты)
app.Configure();

await app.RunAsync();

public partial class Program;
