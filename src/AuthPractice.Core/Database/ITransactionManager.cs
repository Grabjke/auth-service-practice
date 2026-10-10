namespace AuthPractice.Core.Database;

// Одна транзакция на весь хендлер.
// UserStore настроен с AutoSaveChanges = false: UserManager/SignInManager только трекают изменения
// в DbContext, а в БД они уходят одним SaveChangesAsync перед CommitAsync
public interface ITransactionManager
{
    Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

// Без CommitAsync транзакция откатывается при DisposeAsync (await using)
public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
