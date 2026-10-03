using AuthPractice.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 1. Регистрируем сервисы всех слоёв в DI
builder.Services.AddConfiguration(builder.Configuration);

var app = builder.Build();

// 2. Настраиваем pipeline (middleware + маршруты)
app.Configure();

app.Run();

public partial class Program;
