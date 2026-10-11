namespace AuthPractice.Domain.Auth;

// Серверная refresh-сессия: одна строка = один выданный refresh-токен.
// Ротация: предъявленный токен гасится (Revoke + ReplacedBySessionId), выдаётся новый в той же семье.
// Все токены одного входа (login) связаны FamilyId — "token family" из RFC 9700 / Auth0:
// повторное предъявление уже погашенного токена = кража → отзываем всю семью разом
public sealed class RefreshSession
{
    // Для EF
    private RefreshSession()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    // Цепочка ротаций одного входа. Первая сессия семьи: FamilyId == Id
    public Guid FamilyId { get; private set; }

    // SHA-256 сырого токена (64 hex). Сам токен в БД не храним: дамп базы не даёт живых сессий
    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    // Абсолютный срок всего входа: задаётся на login и копируется во все ротации.
    // Без него вор с украденным refresh мог бы ротировать бесконечно, пока жертва не заходит
    public DateTimeOffset FamilyExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public RefreshSessionRevokeReason? RevokeReason { get; private set; }

    // Кем заменена при ротации — следующее звено цепочки
    public Guid? ReplacedBySessionId { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt || now >= FamilyExpiresAt;

    // Новый вход → новая семья. familyLifetime — сколько живёт весь вход, сколько ни ротируй
    public static RefreshSession StartFamily(
        Guid userId,
        string tokenHash,
        DateTimeOffset now,
        TimeSpan lifetime,
        TimeSpan familyLifetime)
    {
        var id = Guid.CreateVersion7(now);
        var familyExpiresAt = now + familyLifetime;
        return new RefreshSession
        {
            Id = id,
            UserId = userId,
            FamilyId = id,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = Min(now + lifetime, familyExpiresAt),
            FamilyExpiresAt = familyExpiresAt,
        };
    }

    // Ротация: гасим текущую и выдаём следующую в той же семье.
    // Срок нового токена продлевается, но не дальше абсолютного срока семьи
    public RefreshSession Rotate(string newTokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        var next = new RefreshSession
        {
            Id = Guid.CreateVersion7(now),
            UserId = UserId,
            FamilyId = FamilyId,
            TokenHash = newTokenHash,
            CreatedAt = now,
            ExpiresAt = Min(now + lifetime, FamilyExpiresAt),
            FamilyExpiresAt = FamilyExpiresAt,
        };

        Revoke(RefreshSessionRevokeReason.Rotated, now);
        ReplacedBySessionId = next.Id;

        return next;
    }

    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;

    // Повторный отзыв ничего не меняет: первая причина и время сохраняются
    public void Revoke(RefreshSessionRevokeReason reason, DateTimeOffset now)
    {
        if (IsRevoked)
            return;

        RevokedAt = now;
        RevokeReason = reason;
    }
}

public enum RefreshSessionRevokeReason
{
    // Погашена при ротации (заменена ReplacedBySessionId)
    Rotated,

    // Пользователь вышел
    Logout,

    // Предъявлен уже погашенный токен этой семьи — отозвана вся семья
    ReuseDetected,

    // Пользователь удалён / заблокирован на момент refresh
    UserInvalid,
}
