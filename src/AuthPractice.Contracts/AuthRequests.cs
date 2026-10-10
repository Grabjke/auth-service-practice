namespace AuthPractice.Contracts;

// POST /api/auth/register
public record RegisterRequest(string UserName, string Email, string Password);

// POST /api/auth/login и POST /api/auth/jwt/login
public record LoginRequest(string Email, string Password);

// Ответ register/login — кто создан / кто вошёл
public record AuthUserResponse(Guid Id, string UserName, string Email);

// Ответ POST /api/auth/jwt/login — access-токен для заголовка "Authorization: Bearer <token>"
public record JwtLoginResponse(string AccessToken, DateTimeOffset ExpiresAt);
