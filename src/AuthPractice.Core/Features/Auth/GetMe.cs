using AuthPractice.Contracts;
using AuthPractice.Core.Auth;
using Core.Abstractions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AuthPractice.Core.Features.Auth;

// GET /api/auth/me — нужен любой аутентифицированный пользователь: валидная auth-cookie ИЛИ Bearer-токен
public sealed class GetMeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("auth/me", async (GetMeHandler handler, CancellationToken ct) =>
                ResultResponse.Ok(await handler.Handle(new GetMeQuery(), ct)))
            // Явно перечисляем схемы: без этого работала бы только default (cookie).
            // Авторизация аутентифицирует обеими и объединяет identities в один HttpContext.User.
            // Ни cookie, ни валидного токена → 401 (cookie: вместо редиректа, Bearer: + WWW-Authenticate)
            .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = AuthSchemes.CookieOrBearer });
}

public sealed record GetMeQuery : IQuery;

// HttpContext.User — его восстановил CookieAuthenticationHandler (из cookie) или JwtBearerHandler (из токена)
public sealed class GetMeHandler(IHttpContextAccessor httpContextAccessor) : IQueryHandler<MeResponse, GetMeQuery>
{
    public Task<MeResponse> Handle(GetMeQuery query, CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext!.User;

        return Task.FromResult(new MeResponse(
            user.GetUserId(),
            user.GetUserName(),
            user.GetEmail(),
            user.GetRoles(),
            user.Identity?.AuthenticationType,
            user.Claims.Select(c => new ClaimDto(c.Type, c.Value)).ToList()));
    }
}
