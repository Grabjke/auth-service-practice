using AuthPractice.Domain.Auth;

namespace AuthPractice.Core.Auth.RefreshTokens;

// Хранилище refresh-сессий (auth.refresh_sessions). Изменения уходят в БД через ITransactionManager
public interface IRefreshSessionRepository
{
    void Add(RefreshSession session);

    // SELECT ... FOR UPDATE: блокирует строку до конца транзакции — параллельный refresh
    // тем же токеном ждёт и видит уже погашенную сессию, а не выдаёт второй токен
    Task<RefreshSession?> GetByTokenHashForUpdateAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefreshSession>> GetActiveInFamilyAsync(Guid familyId, CancellationToken cancellationToken = default);
}
