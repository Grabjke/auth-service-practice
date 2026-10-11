using AuthPractice.Core.Auth.RefreshTokens;
using Core.Abstractions;
using CSharpFunctionalExtensions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using SharedKernel;

namespace AuthPractice.Core.Features.Auth;

// POST /api/auth/jwt/logout — отзывает текущую refresh-сессию и чистит cookie.
// Идемпотентный и без авторизации: учётные данные здесь — сама refresh-cookie
// (access к этому моменту мог уже истечь). Access доживает свой короткий срок
public sealed class JwtLogoutEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("auth/jwt/logout", async (JwtLogoutHandler handler, CancellationToken ct) =>
        {
            await handler.Handle(new JwtLogoutCommand(), ct);
            return ResultResponse.Ok();
        });
}

public sealed record JwtLogoutCommand : ICommand;

public sealed class JwtLogoutHandler(
    RefreshSessionManager refreshSessionManager,
    RefreshTokenCookie refreshTokenCookie)
    : ICommandHandler<JwtLogoutCommand>
{
    public async Task<UnitResult<Errors>> Handle(JwtLogoutCommand command, CancellationToken cancellationToken = default)
    {
        var rawToken = refreshTokenCookie.Read();
        if (rawToken is not null)
            await refreshSessionManager.RevokeAsync(rawToken, cancellationToken);

        refreshTokenCookie.Delete();
        return UnitResult.Success<Errors>();
    }
}
