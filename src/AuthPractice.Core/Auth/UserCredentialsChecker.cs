using AuthPractice.Core.Database;
using AuthPractice.Domain.Users;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using SharedKernel;

namespace AuthPractice.Core.Auth;

// Общая проверка email + пароль для cookie- и JWT-логина.
// Только проверка пароля (+ счётчик неудачных попыток / lockout), ни cookie, ни токен НЕ выдаёт.
// PasswordSignInAsync не используем: он собрал бы клаймы через IUserClaimsPrincipalFactory и поставил cookie
public sealed class UserCredentialsChecker(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    ITransactionManager transactionManager)
{
    private static readonly Error InvalidCredentials =
        Error.Authentication("credentials.invalid", "Неверный email или пароль");

    public async Task<Result<AuthenticatedUser, Errors>> Check(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await transactionManager.BeginTransactionAsync(cancellationToken);

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return InvalidCredentials.ToErrors();

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        // AccessFailedCount / сброс счётчика — одним SaveChanges, даже если пароль неверный
        await transactionManager.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (signInResult.IsLockedOut)
            return Error.Authentication("user.locked_out", "Слишком много попыток, попробуйте позже").ToErrors();
        if (!signInResult.Succeeded)
            return InvalidCredentials.ToErrors();

        var roles = await userManager.GetRolesAsync(user);

        return new AuthenticatedUser(user, roles.ToList());
    }
}

public sealed record AuthenticatedUser(User User, IReadOnlyList<string> Roles);
