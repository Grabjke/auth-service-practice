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

// POST /api/auth/jwt/login — параллель cookie-логину для API-клиентов (mobile / сервисы).
// Та же проверка пароля + lockout. Ответ: короткий access в JSON (клиент шлёт его в "Authorization: Bearer")
// + долгий refresh в HttpOnly-cookie (Set-Cookie). Access и refresh намеренно лежат в разных местах
public sealed class JwtLoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("auth/jwt/login", async (LoginRequest request, JwtLoginHandler handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new JwtLoginCommand(request.Email, request.Password), ct);

            // ErrorType.AUTHENTICATION в Framework маппится в 500, поэтому 401 отдаём сами
            return result.IsSuccess
                ? ResultResponse.Ok(result.Value)
                : Results.Json(Envelope.Error(result.Error), statusCode: StatusCodes.Status401Unauthorized);
        });
}

public sealed record JwtLoginCommand(string Email, string Password) : ICommand;

public sealed class JwtLoginHandler(
    UserCredentialsChecker credentialsChecker,
    IJwtTokenService jwtTokenService,
    RefreshSessionManager refreshSessionManager,
    RefreshTokenCookie refreshTokenCookie)
    : ICommandHandler<JwtLoginResponse, JwtLoginCommand>
{
    public async Task<Result<JwtLoginResponse, Errors>> Handle(
        JwtLoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var checkResult = await credentialsChecker.Check(command.Email, command.Password, cancellationToken);
        if (checkResult.IsFailure)
            return checkResult.Error;

        var (user, roles) = checkResult.Value;

        // Access сервер не хранит: валидность = подпись + exp, отозвать нельзя → живёт минуты
        var token = jwtTokenService.Generate(user, roles);

        // Refresh — серверная сессия (новая семья), в БД только хэш, сырой токен — в cookie
        var refreshToken = await refreshSessionManager.StartAsync(user, cancellationToken);
        refreshTokenCookie.Write(refreshToken.RawToken, refreshToken.ExpiresAt);

        return new JwtLoginResponse(token.Token, token.ExpiresAt);
    }
}
