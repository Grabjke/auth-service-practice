using AuthPractice.Core.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace AuthPractice.Infrastructure.Authentication;

// Аутентифицирован ли пользователь? Если да — заполняем scoped UserScopeData.
// Ставится ПОСЛЕ UseAuthentication и ДО UseAuthorization.
// Анонимный запрос / невалидный токен — просто идём дальше: UserScopeData остаётся пустым,
// а 401 на защищённых endpoint'ах вернёт авторизация
public sealed class UserScopeDataMiddleware(RequestDelegate next)
{
    // UserScopeData — scoped, поэтому берём его параметром InvokeAsync (из scope запроса), а не в конструкторе
    public async Task InvokeAsync(HttpContext context, UserScopeData userScopeData)
    {
        // 1. Cookie: UseAuthentication уже прогнал default-схему и положил результат в HttpContext.User
        if (context.User.Identity?.IsAuthenticated == true)
        {
            userScopeData.Fill(context.User, AuthSchemes.Cookie);
        }
        // 2. Bearer — не default, UseAuthentication его не трогал, аутентифицируем явно.
        // Результат кешируется хендлером на запрос: авторизация потом не будет проверять токен заново.
        // HttpContext.User НЕ подменяем — иначе endpoint'ы "только cookie" ([Authorize] без схем) пустили бы Bearer
        else
        {
            var bearer = await context.AuthenticateAsync(AuthSchemes.Bearer);
            if (bearer.Succeeded)
                userScopeData.Fill(bearer.Principal, AuthSchemes.Bearer);
        }

        await next(context);
    }
}

public static class UserScopeDataMiddlewareExtensions
{
    public static IApplicationBuilder UseUserScopeData(this IApplicationBuilder app) =>
        app.UseMiddleware<UserScopeDataMiddleware>();
}
