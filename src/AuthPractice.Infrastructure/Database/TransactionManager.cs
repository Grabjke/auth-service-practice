using AuthPractice.Core.Database;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuthPractice.Infrastructure.Database;

// Scoped: тот же AppIdentityDbContext, что внутри UserStore/RoleStore → менеджеры Identity
// работают в этой же транзакции
public sealed class TransactionManager(AppIdentityDbContext dbContext) : ITransactionManager
{
    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new DbTransactionScope(await dbContext.Database.BeginTransactionAsync(cancellationToken));

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private sealed class DbTransactionScope(IDbContextTransaction transaction) : ITransactionScope
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) =>
            transaction.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken = default) =>
            transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
