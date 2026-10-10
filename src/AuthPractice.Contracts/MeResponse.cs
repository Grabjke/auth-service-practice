namespace AuthPractice.Contracts;

// Кто я: всё берётся из HttpContext.User (ClaimsPrincipal из cookie или Bearer-токена), без похода в БД
public record MeResponse(
    string? Id,
    string? UserName,
    string? Email,
    IReadOnlyList<string> Roles,
    string? AuthenticationType,
    IReadOnlyList<ClaimDto> Claims);

public record ClaimDto(string Type, string Value);
