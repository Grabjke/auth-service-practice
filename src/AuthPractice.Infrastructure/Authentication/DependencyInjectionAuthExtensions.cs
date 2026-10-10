using System.IdentityModel.Tokens.Jwt;
using AuthPractice.Core.Auth;
using AuthPractice.Domain.Users;
using AuthPractice.Infrastructure.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AuthPractice.Infrastructure.Authentication;

public static class DependencyInjectionAuthExtensions
{
    public static IServiceCollection AddIdentityAuthentication(this IServiceCollection services)
    {
        // Входящие sub/email/name НЕ переписываем в длинные legacy-URI ClaimTypes
        // ("http://schemas.xmlsoap.org/.../nameidentifier"): principal из Bearer должен совпадать с cookie-principal.
        // Глобально и до создания JwtBearerOptions — их дефолт MapInboundClaims читается из этого флага
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
        JsonWebTokenHandler.DefaultMapInboundClaims = false; // JwtBearer в .NET 8+ валидирует через JsonWebTokenHandler

        // Свой UserStore (AutoSaveChanges = false) — сохранение только через ITransactionManager.
        // Регистрируем до AddEntityFrameworkStores — там TryAdd, наш не перезапишется
        services.AddScoped<IUserStore<User>, DeferredSaveUserStore>();

        // AddIdentityCore (а не AddIdentity) — без своих cookie-схем Identity.Application/External
        // и без подмены default-схемы: все схемы регистрируем сами ниже
        services
            .AddIdentityCore<User>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppIdentityDbContext>()
            .AddSignInManager();

        // IHttpContextAccessor — хендлеры Core берут через него HttpContext (User, SignInAsync)
        services.AddHttpContextAccessor();

        services.AddJwtOptions();

        services
            // 1. DefaultScheme = Cookies (веб-фронт). [Authorize] / RequireAuthorization() без схем
            // по-прежнему работают только через cookie — default НЕ меняем, иначе сломается веб
            .AddAuthentication(AuthSchemes.Cookie)
            .AddCookie(AuthSchemes.Cookie, options =>
            {
                options.Cookie.Name = "auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // в Docker ходим по http
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;

                // Это API, а не MVC: вместо 302 на /Account/Login отдаём коды
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            })
            // 2. Bearer — вторая, ИМЕНОВАННАЯ схема (mobile / сервисы), регистрируется ПОСЛЕ cookie.
            // Default остаётся Cookies (задан явно в AddAuthentication выше). Endpoint выбирает Bearer явно:
            // AuthenticationSchemes = AuthSchemes.Bearer или AuthSchemes.CookieOrBearer.
            // Параметры валидации берутся из IOptions<JwtOptions> — в AddJwtOptions → ConfigureJwtBearer
            .AddJwtBearer(AuthSchemes.Bearer);

        // Политики: правила "кому можно" поверх уже аутентифицированного пользователя.
        // RequireRole смотрит на RoleClaimType = AuthClaims.Role (одинаковый у cookie и Bearer).
        // AddAuthenticationSchemes — админка доступна и веб-фронту (cookie), и API-клиентам (Bearer)
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.AdminOnly, policy => policy
                .AddAuthenticationSchemes(AuthSchemes.Cookie, AuthSchemes.Bearer)
                .RequireRole(UserRoles.Admin));

        return services;
    }

    private static IServiceCollection AddJwtOptions(this IServiceCollection services)
    {
        // Options pattern: секция "Jwt" из appsettings*.json → IOptions<JwtOptions>.
        // ValidateOnStart: нет ключа / ключ < 32 символов / ExpireMinutes > 30 → сервис падает на старте,
        // а не на первом логине
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // JwtBearerOptions настраиваем из IOptions<JwtOptions> (лениво, при первом запросе к схеме),
        // а не читаем конфиг вручную в момент регистрации
        services.AddOptions<JwtBearerOptions>(AuthSchemes.Bearer)
            .Configure<IOptions<JwtOptions>>(ConfigureJwtBearer);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }

    private static void ConfigureJwtBearer(JwtBearerOptions options, IOptions<JwtOptions> jwtOptions)
    {
        var jwt = jwtOptions.Value;

        // Дублируем глобальный флаг явно — на случай, если опции создадутся раньше
        options.MapInboundClaims = false;

        // Ключ локальный (HS256), без OIDC-discovery через Authority
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = jwt.Audience,

            // exp обязателен и проверяется. ClockSkew = 0: дефолтные 5 минут — запас на рассинхрон часов
            // между серверами; у нас один сервис, лишнее окно жизни токена не нужно
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero,

            // Подпись обязательна и только HS256 — токен с alg=none / другим алгоритмом отклоняется
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            IssuerSigningKey = JwtTokenService.CreateSigningKey(jwt.SigningKey),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],

            // Те же короткие клаймы, что у cookie-principal: Identity.Name и IsInRole/RequireRole
            // работают одинаково для обеих схем
            NameClaimType = AuthClaims.UserName,
            RoleClaimType = AuthClaims.Role,
            // Identity.AuthenticationType = "Bearer" (как "Cookies" у cookie), а не дефолтный "AuthenticationTypes.Federation"
            AuthenticationType = AuthSchemes.Bearer,
        };
    }
}
