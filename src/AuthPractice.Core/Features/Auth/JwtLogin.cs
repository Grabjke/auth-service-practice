using AuthPractice.Contracts;
using AuthPractice.Core.Auth;
using Core.Abstractions;
using CSharpFunctionalExtensions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;

namespace AuthPractice.Core.Features.Auth;

// POST /api/auth/jwt/login — параллель cookie-логину для API-клиентов (mobile / сервисы).
// Та же проверка пароля + lockout, но в ответ — access-токен в теле, без Set-Cookie.
// Клиент дальше шлёт его в "Authorization: Bearer <token>"
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
    IJwtTokenService jwtTokenService)
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

        // Сервер токен не хранит: валидность = подпись + exp. Отозвать до exp нельзя → токен короткоживущий
        var token = jwtTokenService.Generate(user, roles);

        return new JwtLoginResponse(token.Token, token.ExpiresAt);
    }
}
