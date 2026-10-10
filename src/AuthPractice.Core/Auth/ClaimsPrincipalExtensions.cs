using System.Security.Claims;
using AuthPractice.Domain.Users;

namespace AuthPractice.Core.Auth;

public static class ClaimsPrincipalExtensions
{
    // Собираем principal сами (вместо автоматического IUserClaimsPrincipalFactory из Identity) —
    // в cookie попадут ровно эти клаймы
    public static ClaimsPrincipal ToClaimsPrincipal(
        this User user,
        IEnumerable<string> roles,
        string authenticationType)
    {
        var claims = new List<Claim>
        {
            new(AuthClaims.Id, user.Id.ToString()),
            new(AuthClaims.UserName, user.UserName!),
            new(AuthClaims.Email, user.Email!),
            // Меняется при смене пароля/ролей — нужен для server-side проверок и согласованности с JWT
            new(AuthClaims.SecurityStamp, user.SecurityStamp!),
        };
        claims.AddRange(roles.Select(role => new Claim(AuthClaims.Role, role)));

        // nameType/roleType — чтобы Identity.Name и IsInRole/RequireRole смотрели на наши клаймы
        var identity = new ClaimsIdentity(claims, authenticationType, AuthClaims.UserName, AuthClaims.Role);
        return new ClaimsPrincipal(identity);
    }

    public static string? GetUserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(AuthClaims.Id);

    public static string? GetUserName(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(AuthClaims.UserName);

    public static string? GetEmail(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(AuthClaims.Email);

    public static string? GetSecurityStamp(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(AuthClaims.SecurityStamp);

    public static IReadOnlyList<string> GetRoles(this ClaimsPrincipal principal) =>
        principal.FindAll(AuthClaims.Role).Select(c => c.Value).ToList();
}
