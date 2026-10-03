using System.Security.Claims;
using AuthPractice.Contracts;
using Core.Abstractions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AuthPractice.Core.Features.Auth;

// GET /api/auth/me — нужен любой аутентифицированный пользователь
public sealed class GetMeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("auth/me", async (ClaimsPrincipal user, GetMeHandler handler, CancellationToken ct) =>
                ResultResponse.Ok(await handler.Handle(new GetMeQuery(user), ct)))
            // Без этого endpoint публичный. С ним — UseAuthorization вызовет наш handler
            .RequireAuthorization();
}

// ClaimsPrincipal = HttpContext.User, его заполнил TestAuthenticationHandler
public sealed record GetMeQuery(ClaimsPrincipal User) : IQuery;

public sealed class GetMeHandler : IQueryHandler<MeResponse, GetMeQuery>
{
    public Task<MeResponse> Handle(GetMeQuery query, CancellationToken cancellationToken = default) =>
        Task.FromResult(new MeResponse(
            query.User.Identity?.Name,
            query.User.Identity?.AuthenticationType,
            query.User.Claims.Select(c => new ClaimDto(c.Type, c.Value)).ToList()));
}
