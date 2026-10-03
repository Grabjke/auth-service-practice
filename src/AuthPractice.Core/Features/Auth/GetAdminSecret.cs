using AuthPractice.Core.Abstractions;
using AuthPractice.Core.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AuthPractice.Core.Features.Auth;

// GET /api/auth/admin — только роль admin.
// user-токен → 403 (аутентифицирован, но нет прав), без токена → 401
public sealed class GetAdminSecretEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("auth/admin", () => Results.Ok(new { secret = "только для админа" }))
            .RequireAuthorization(AuthPolicies.AdminOnly);
}
