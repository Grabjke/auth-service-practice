namespace AuthPractice.Contracts;

// Кто я: данные из ClaimsPrincipal, который создал наш AuthenticationHandler
public record MeResponse(string? Name, string? AuthenticationType, IReadOnlyList<ClaimDto> Claims);

public record ClaimDto(string Type, string Value);
