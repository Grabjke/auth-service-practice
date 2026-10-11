using AuthPractice.Core.Database;
using AuthPractice.Domain.Auth;
using AuthPractice.Domain.Users;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace AuthPractice.Core.Auth.RefreshTokens;

// Жизненный цикл refresh-сессий: выдача на login, ротация на refresh (с reuse detection), отзыв на logout.
// Наружу отдаёт сырой токен — только для записи в cookie; в БД попадает лишь его SHA-256
public sealed class RefreshSessionManager(
    IRefreshSessionRepository repository,
    ITransactionManager transactionManager,
    UserManager<User> userManager,
    TimeProvider timeProvider,
    IOptions<RefreshTokenOptions> options,
    ILogger<RefreshSessionManager> logger)
{
    private readonly RefreshTokenOptions _options = options.Value;

    // Login: новая семья. Пароль уже проверен, вызывается вне транзакции проверки
    public async Task<IssuedRefreshToken> StartAsync(User user, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var rawToken = RefreshToken.Generate();

        var session = RefreshSession.StartFamily(
            user.Id, RefreshToken.Hash(rawToken), now, _options.Lifetime, _options.FamilyLifetime);
        repository.Add(session);
        await transactionManager.SaveChangesAsync(cancellationToken);

        return new IssuedRefreshToken(rawToken, session.ExpiresAt);
    }

    // Refresh: проверка, отзыв и выдача — в одной транзакции под блокировкой строки.
    // Иначе два параллельных запроса одним токеном оба прошли бы проверку и выдали два токена
    public async Task<Result<RotatedRefreshToken, Errors>> RotateAsync(
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        await using var transaction = await transactionManager.BeginTransactionAsync(cancellationToken);

        var session = await repository.GetByTokenHashForUpdateAsync(RefreshToken.Hash(rawToken), cancellationToken);
        if (session is null)
            return RefreshTokenErrors.InvalidToken.ToErrors();

        if (session.IsRevoked)
        {
            // Гонка вкладок: токен погашен ротацией секунду назад — это не кража
            if (session.RevokeReason == RefreshSessionRevokeReason.Rotated
                && now - session.RevokedAt!.Value <= TimeSpan.FromSeconds(_options.ConcurrentRefreshGraceSeconds))
            {
                return RefreshTokenErrors.ConcurrentRefresh.ToErrors();
            }

            // Reuse detection: погашенный токен предъявлен снова → его украли (или украденным уже
            // воспользовались). Кто из двоих жертва — не знаем, поэтому обрываем всю семью у обоих
            if (session.RevokeReason != RefreshSessionRevokeReason.Logout)
            {
                logger.LogWarning(
                    "Refresh token reuse detected: session {SessionId}, family {FamilyId}, user {UserId}",
                    session.Id, session.FamilyId, session.UserId);
                await RevokeFamilyAsync(session.FamilyId, RefreshSessionRevokeReason.ReuseDetected, now, cancellationToken);
                await transactionManager.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            return RefreshTokenErrors.InvalidToken.ToErrors();
        }

        // Истёк сам токен или весь вход (FamilyExpiresAt) — только заново по паролю
        if (session.IsExpired(now))
            return RefreshTokenErrors.InvalidToken.ToErrors();

        // Access выдаём со свежими данными из БД: удалённому / заблокированному — нет
        var user = await userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            await RevokeFamilyAsync(session.FamilyId, RefreshSessionRevokeReason.UserInvalid, now, cancellationToken);
            await transactionManager.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return RefreshTokenErrors.InvalidToken.ToErrors();
        }

        var newRawToken = RefreshToken.Generate();
        var next = session.Rotate(RefreshToken.Hash(newRawToken), now, _options.Lifetime);
        repository.Add(next);

        await transactionManager.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var roles = await userManager.GetRolesAsync(user);

        return new RotatedRefreshToken(user, roles.ToList(), new IssuedRefreshToken(newRawToken, next.ExpiresAt));
    }

    // Logout завершает ВХОД, а не одну строку: гасим всю семью (другие устройства — свои семьи, их не трогаем).
    // Уже выданный access доживёт свой короткий срок — цена stateless-схемы (чёрного списка access нет намеренно)
    public async Task RevokeAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        await using var transaction = await transactionManager.BeginTransactionAsync(cancellationToken);

        var session = await repository.GetByTokenHashForUpdateAsync(RefreshToken.Hash(rawToken), cancellationToken);
        if (session is null)
            return;

        // В cookie уже ротированный токен, а живой токен семьи у кого-то другого — его украли и обменяли.
        // Жертва жмёт «Выйти» → обрываем и сессию вора
        var reason = RefreshSessionRevokeReason.Logout;
        if (session.RevokeReason == RefreshSessionRevokeReason.Rotated)
        {
            logger.LogWarning(
                "Refresh token reuse detected on logout: session {SessionId}, family {FamilyId}, user {UserId}",
                session.Id, session.FamilyId, session.UserId);
            reason = RefreshSessionRevokeReason.ReuseDetected;
        }

        // Живых сессий в семье нет (logout / reuse уже были) — RevokeFamilyAsync просто ничего не найдёт
        await RevokeFamilyAsync(session.FamilyId, reason, now, cancellationToken);

        await transactionManager.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task RevokeFamilyAsync(
        Guid familyId,
        RefreshSessionRevokeReason reason,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        foreach (var familySession in await repository.GetActiveInFamilyAsync(familyId, cancellationToken))
            familySession.Revoke(reason, now);
    }
}

public sealed record IssuedRefreshToken(string RawToken, DateTimeOffset ExpiresAt);

public sealed record RotatedRefreshToken(User User, IReadOnlyList<string> Roles, IssuedRefreshToken RefreshToken);
