using AuthPractice.Contracts;
using AuthPractice.Core.Auth;
using AuthPractice.Core.Auth.RefreshTokens;
using Core.Abstractions;
using CSharpFunctionalExtensions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;

namespace AuthPractice.Core.Features.Auth;

// POST /api/auth/jwt/refresh — refresh из HttpOnly-cookie → новый access (JSON) + новый refresh (Set-Cookie).
// Предъявленный refresh гасится (ротация). Без тела: refresh никогда не ходит в JSON
public sealed class JwtRefreshEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("auth/jwt/refresh", async (JwtRefreshHandler handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new JwtRefreshCommand(), ct);
            if (result.IsSuccess)
                return ResultResponse.Ok(result.Value);

            // ErrorType.AUTHENTICATION в Framework маппится в 500, поэтому коды отдаём сами
            var statusCode = result.Error.Any(e => e.Type == ErrorType.CONFLICT)
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status401Unauthorized;
            return Results.Json(Envelope.Error(result.Error), statusCode: statusCode);
        });
}

public sealed record JwtRefreshCommand : ICommand;

public sealed class JwtRefreshHandler(
    RefreshSessionManager refreshSessionManager,
    RefreshTokenCookie refreshTokenCookie,
    IJwtTokenService jwtTokenService)
    : ICommandHandler<JwtLoginResponse, JwtRefreshCommand>
{
    public async Task<Result<JwtLoginResponse, Errors>> Handle(
        JwtRefreshCommand command,
        CancellationToken cancellationToken = default)
    {
        var rawToken = refreshTokenCookie.Read();
        if (rawToken is null)
            return RefreshTokenErrors.InvalidToken.ToErrors();

        var rotateResult = await refreshSessionManager.RotateAsync(rawToken, cancellationToken);
        if (rotateResult.IsFailure)
        {
            // 409 (гонка вкладок): cookie не трогаем — её уже заменил ответ параллельного запроса.
            // 401: сессии нет / истекла / отозвана — чистим cookie, клиенту дорога только на логин
            if (rotateResult.Error.All(e => e.Type != ErrorType.CONFLICT))
                refreshTokenCookie.Delete();

            return rotateResult.Error;
        }

        var (user, roles, refreshToken) = rotateResult.Value;
        refreshTokenCookie.Write(refreshToken.RawToken, refreshToken.ExpiresAt);

        var accessToken = jwtTokenService.Generate(user, roles);
        return new JwtLoginResponse(accessToken.Token, accessToken.ExpiresAt);
    }
}
