namespace AuthPractice.Contracts;

// Ответ GET /api/users/me — профиль для UI. Служебное (схема аутентификации, security stamp,
// lockout, счётчик неудачных попыток) наружу не отдаём
public record UserProfileResponse(
    Guid Id,
    string UserName,
    string Email,
    bool EmailConfirmed,
    string? PhoneNumber,
    IReadOnlyList<string> Roles,
    bool TwoFactorEnabled);
