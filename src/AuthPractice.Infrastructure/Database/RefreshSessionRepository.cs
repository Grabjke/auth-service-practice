using AuthPractice.Core.Auth.RefreshTokens;
using AuthPractice.Domain.Auth;
using Microsoft.EntityFrameworkCore;

namespace AuthPractice.Infrastructure.Database;

public sealed class RefreshSessionRepository(AppIdentityDbContext dbContext) : IRefreshSessionRepository
{
    public void Add(RefreshSession session) => dbContext.RefreshSessions.Add(session);

    public async Task<RefreshSession?> GetByTokenHashForUpdateAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        // FOR UPDATE держит строку до конца транзакции: параллельный refresh тем же токеном
        // ждёт commit'а первого и затем видит уже погашенную сессию.
        // ToListAsync, а не SingleOrDefault: так EF не оборачивает FromSql в подзапрос
        var sessions = await dbContext.RefreshSessions
            .FromSql($"SELECT * FROM auth.refresh_sessions WHERE token_hash = {tokenHash} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return sessions.SingleOrDefault();
    }

    public async Task<IReadOnlyList<RefreshSession>> GetActiveInFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default) =>
        await dbContext.RefreshSessions
            .Where(s => s.FamilyId == familyId && s.RevokedAt == null)
            .ToListAsync(cancellationToken);
}
