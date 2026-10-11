using System.ComponentModel.DataAnnotations;

namespace AuthPractice.Infrastructure.Authentication;

// Options pattern: биндится из секции "Jwt" (appsettings*.json), внедряется как IOptions<JwtOptions>.
// Проверки — DataAnnotations + ValidateOnStart (см. AddJwtOptions)
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = null!;

    [Required]
    public string Audience { get; init; } = null!;

    // Access нельзя отозвать до exp → его TTL и есть окно полезности украденного токена.
    // Долгую сессию держит refresh-токен, поэтому access — минуты
    [Range(1, 15)]
    public int ExpireMinutes { get; init; }

    // HS256 = HMAC-SHA256: ключ не короче выхода хеша (32 байта), иначе подпись упадёт в рантайме
    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = null!;
}
