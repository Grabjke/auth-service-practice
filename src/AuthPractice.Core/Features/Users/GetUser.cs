using AuthPractice.Contracts;
using AuthPractice.Core.Auth;
using AuthPractice.Domain.Users;
using Core.Abstractions;
using CSharpFunctionalExtensions;
using Framework.EndpointResult;
using Framework.Endpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using SharedKernel;

namespace AuthPractice.Core.Features.Users;

// GET /api/users/me — профиль текущего пользователя (cookie или Bearer).
// Кто запрашивает — берём из UserScopeData (DI), сам профиль — из БД
public sealed class GetUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("users/me", async (GetUserHandler handler, CancellationToken ct) =>
                (EndpointResult<UserProfileResponse>)await handler.Handle(new GetUserQuery(), ct))
            .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = AuthSchemes.CookieOrBearer });
}

public sealed record GetUserQuery : IQuery;

public sealed class GetUserHandler(UserScopeData userScopeData, UserManager<User> userManager)
    : IQueryHandlerWithResult<UserProfileResponse, GetUserQuery>
{
    public async Task<Result<UserProfileResponse, Errors>> Handle(
        GetUserQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = userScopeData.GetRequiredUserId();

        // Токен/cookie мог пережить удаление пользователя — claims не знают о БД
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Error.NotFound("user.not_found", "Пользователь не найден").ToErrors();

        // Всё — свежее из БД (роли в claims могут устареть до exp токена)
        var roles = await userManager.GetRolesAsync(user);

        return new UserProfileResponse(
            user.Id,
            user.UserName!,
            user.Email!,
            user.EmailConfirmed,
            user.PhoneNumber,
            roles.ToList(),
            user.TwoFactorEnabled);
    }
}
