using AuthPractice.Domain.Users;

namespace AuthPractice.Core.Auth;

// Выпуск access-токена (JWT). Реализация — в Infrastructure
public interface IJwtTokenService
{
    JwtAccessToken Generate(User user, IEnumerable<string> roles);
}

public sealed record JwtAccessToken(string Token, DateTimeOffset ExpiresAt);
