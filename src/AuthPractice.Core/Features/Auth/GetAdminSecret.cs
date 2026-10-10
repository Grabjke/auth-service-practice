using AuthPractice.Core.Auth;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AuthPractice.Core.Features.Auth;

// GET /api/auth/admin — только роль admin.
// роль user → 403 (аутентифицирован, но нет прав), без cookie/токена → 401.
// Схемы (cookie + Bearer) заданы в самой политике AdminOnly
public sealed class GetAdminSecretEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("auth/admin", () => ResultResponse.Ok(new { secret = "только для админа" }))
            .RequireAuthorization(AuthPolicies.AdminOnly);
}
