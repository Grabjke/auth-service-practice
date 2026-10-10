using AuthPractice.Contracts;
using AuthPractice.Core.Auth;
using AuthPractice.Core.Database;
using AuthPractice.Domain.Users;
using Core.Abstractions;
using CSharpFunctionalExtensions;
using Framework.EndpointResult;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SharedKernel;

namespace AuthPractice.Core.Features.Auth;

// POST /api/auth/register — создаёт пользователя с ролью user (без входа)
public sealed class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("auth/register", async (RegisterRequest request, RegisterHandler handler, CancellationToken ct) =>
            (EndpointResult<AuthUserResponse>)await handler.Handle(
                new RegisterCommand(request.UserName, request.Email, request.Password), ct));
}

public sealed record RegisterCommand(string UserName, string Email, string Password) : ICommand;

public sealed class RegisterHandler(
    UserManager<User> userManager,
    ITransactionManager transactionManager)
    : ICommandHandler<AuthUserResponse, RegisterCommand>
{
    public async Task<Result<AuthUserResponse, Errors>> Handle(
        RegisterCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await transactionManager.BeginTransactionAsync(cancellationToken);

        var user = User.Create(command.UserName, command.Email);

        // Валидация (уникальность, пароль) + Add в DbContext. В БД пока ничего не пишется
        var createResult = await userManager.CreateAsync(user, command.Password);
        if (!createResult.Succeeded)
            return createResult.Errors.ToErrors();

        var roleResult = await userManager.AddToRoleAsync(user, UserRoles.User);
        if (!roleResult.Succeeded)
            return roleResult.Errors.ToErrors();

        // Пользователь + его роль уходят в БД одним SaveChanges
        await transactionManager.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AuthUserResponse(user.Id, user.UserName!, user.Email!);
    }
}
