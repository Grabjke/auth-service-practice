using System.Security.Claims;
using System.Text;
using AuthPractice.Core.Auth;
using AuthPractice.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AuthPractice.Infrastructure.Authentication;

// Выпуск access-токена: HS256 (симметричный ключ) — достаточно для одного сервиса.
// RSA + key rotation понадобятся, когда токены будут проверять другие сервисы
public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public JwtAccessToken Generate(User user, IEnumerable<string> roles)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.ExpireMinutes);

        // Те же клаймы (и те же короткие имена), что кладём в cookie через ToClaimsPrincipal:
        // sub, name, email, security_stamp, role
        var claims = user.ToClaimsPrincipal(roles, AuthSchemes.Bearer).Claims.ToList();
        // jti — уникальный id токена (пригодится для логов / будущего denylist)
        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,     // iss
            Audience = _options.Audience, // aud
            IssuedAt = now.UtcDateTime,   // iat
            NotBefore = now.UtcDateTime,  // nbf
            Expires = expiresAt.UtcDateTime, // exp
            SigningCredentials = new SigningCredentials(
                CreateSigningKey(_options.SigningKey),
                SecurityAlgorithms.HmacSha256),
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        // exp в токене — в целых секундах, отдаём клиенту то же значение
        return new JwtAccessToken(token, DateTimeOffset.FromUnixTimeSeconds(expiresAt.ToUnixTimeSeconds()));
    }

    // Один и тот же ключ для подписи (здесь) и проверки (JwtBearerOptions)
    public static SymmetricSecurityKey CreateSigningKey(string signingKey) =>
        new(Encoding.UTF8.GetBytes(signingKey));
}
