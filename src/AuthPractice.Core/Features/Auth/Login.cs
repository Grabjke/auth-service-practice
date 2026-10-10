using AuthPractice.Contracts;
using AuthPractice.Core.Auth;
using Core.Abstractions;
using CSharpFunctionalExtensions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;

namespace AuthPractice.Core.Features.Auth;

// POST /api/auth/login — проверяет пароль и выдаёт auth-cookie (веб-фронт)
public sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("auth/login", async (LoginRequest request, LoginHandler handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new LoginCommand(request.Email, request.Password), ct);

            // ErrorType.AUTHENTICATION в Framework маппится в 500, поэтому 401 отдаём сами
            return result.IsSuccess
                ? ResultResponse.Ok(result.Value)
                : Results.Json(Envelope.Error(result.Error), statusCode: StatusCodes.Status401Unauthorized);
        });
}

public sealed record LoginCommand(string Email, string Password) : ICommand;

public sealed class LoginHandler(
    UserCredentialsChecker credentialsChecker,
    IHttpContextAccessor httpContextAccessor)
    : ICommandHandler<AuthUserResponse, LoginCommand>
{
    public async Task<Result<AuthUserResponse, Errors>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var checkResult = await credentialsChecker.Check(command.Email, command.Password, cancellationToken);
        if (checkResult.IsFailure)
            return checkResult.Error;

        var (user, roles) = checkResult.Value;

        // Клаймы — через наш extension, затем cookie-схема сериализует principal в cookie
        var principal = user.ToClaimsPrincipal(roles, AuthSchemes.Cookie);
        await httpContextAccessor.HttpContext!.SignInAsync(AuthSchemes.Cookie, principal);

        return new AuthUserResponse(user.Id, user.UserName!, user.Email!);
    }
}
