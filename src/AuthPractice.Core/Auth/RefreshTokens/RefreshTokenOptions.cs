using System.ComponentModel.DataAnnotations;

namespace AuthPractice.Core.Auth.RefreshTokens;

// Options pattern: секция "RefreshToken" из appsettings*.json → IOptions<RefreshTokenOptions>
public sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";

    // Срок жизни одного refresh-токена (при каждой ротации отсчитывается заново, но не дальше FamilyLifetimeDays)
    [Range(1, 90)]
    public int LifetimeDays { get; init; }

    // Абсолютный срок входа: через столько дней после login нужен пароль, сколько ни обновляйся.
    // Ограничивает, как долго вор с украденным refresh может сидеть, если жертва не заходит
    [Range(1, 365)]
    public int FamilyLifetimeDays { get; init; }

    [Required]
    public string CookieName { get; init; } = null!;

    // Сужает, где браузер шлёт cookie (refresh + logout). Это уменьшает поверхность утечки,
    // но границу безопасности держат HttpOnly + Secure + SameSite, а не Path
    [Required]
    public string CookiePath { get; init; } = null!;

    // Окно, в котором повторное предъявление только что ротированного токена считаем гонкой
    // (две вкладки обновились одновременно), а не кражей: отвечаем 409 без отзыва семьи
    [Range(0, 60)]
    public int ConcurrentRefreshGraceSeconds { get; init; }

    public TimeSpan Lifetime => TimeSpan.FromDays(LifetimeDays);

    public TimeSpan FamilyLifetime => TimeSpan.FromDays(FamilyLifetimeDays);
}
