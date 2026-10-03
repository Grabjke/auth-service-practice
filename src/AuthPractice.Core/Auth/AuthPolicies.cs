namespace AuthPractice.Core.Auth;

// Имена политик авторизации. Endpoints ссылаются на них,
// а сами правила политик регистрируются в Infrastructure
public static class AuthPolicies
{
    public const string AdminOnly = "AdminOnly";
}
