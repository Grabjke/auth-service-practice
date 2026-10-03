using System.Security.Claims;
using AuthPractice.Contracts;
using AuthPractice.Core.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AuthPractice.Core.Features.Auth;

// GET /api/auth/me — нужен любой аутентифицированный пользователь
public sealed class GetMeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("auth/me", (ClaimsPrincipal user, GetMeHandler handler) => Results.Ok(handler.Handle(user)))
            // Без этого endpoint публичный. С ним — UseAuthorization вызовет наш handler
            .RequireAuthorization();
}

public sealed class GetMeHandler
{
    // ClaimsPrincipal = HttpContext.User, его заполнил TestAuthenticationHandler
    public MeResponse Handle(ClaimsPrincipal user) => new(
        user.Identity?.Name,
        user.Identity?.AuthenticationType,
        user.Claims.Select(c => new ClaimDto(c.Type, c.Value)).ToList());
}
