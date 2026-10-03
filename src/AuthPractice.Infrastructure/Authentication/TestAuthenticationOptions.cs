using Microsoft.AspNetCore.Authentication;

namespace AuthPractice.Infrastructure.Authentication;

// Настройки схемы. Наследуемся от AuthenticationSchemeOptions —
// так ASP.NET сможет отдать их в handler через свойство Options
public sealed class TestAuthenticationOptions : AuthenticationSchemeOptions
{
    // Заголовок, из которого читаем токен
    public string HeaderName { get; set; } = "X-Test-Token";

    // "База" токенов: токен -> пользователь (заполняется из appsettings.json)
    public Dictionary<string, TestUser> Users { get; set; } = new();

    // Вызывается фреймворком при первом получении Options — ловим кривой конфиг сразу
    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(HeaderName))
            throw new InvalidOperationException($"{nameof(HeaderName)} must be set");
    }
}

public sealed class TestUser
{
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
}
