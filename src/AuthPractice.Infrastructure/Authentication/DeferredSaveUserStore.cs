using AuthPractice.Domain.Users;
using AuthPractice.Infrastructure.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthPractice.Infrastructure.Authentication;

// UserStore без автосохранения: UserManager/SignInManager только меняют DbContext,
// а в БД всё уходит через ITransactionManager.SaveChangesAsync в хендлере
public sealed class DeferredSaveUserStore : UserStore<User, IdentityRole<Guid>, AppIdentityDbContext, Guid>
{
    public DeferredSaveUserStore(AppIdentityDbContext context, IdentityErrorDescriber? describer = null)
        : base(context, describer)
    {
        AutoSaveChanges = false;
    }

    public override Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        // Базовый UpdateAsync делает Context.Update(user): ещё не сохранённый (Added) пользователь
        // стал бы Modified → UPDATE вместо INSERT (например, CreateAsync + AddToRoleAsync до SaveChanges)
        if (Context.Entry(user).State == EntityState.Added)
        {
            user.ConcurrencyStamp = Guid.NewGuid().ToString();
            return Task.FromResult(IdentityResult.Success);
        }

        return base.UpdateAsync(user, cancellationToken);
    }
}
