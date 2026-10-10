namespace AuthPractice.Core.Auth;

// Свои короткие имена клаймов вместо
// "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" и т.п.
// Совпадают с зарегистрированными именами JWT (RFC 7519 / OIDC: sub, name, email),
// поэтому principal из cookie и из Bearer-токена выглядит одинаково
public static class AuthClaims
{
    public const string Id = "sub";
    public const string UserName = "name";
    public const string Email = "email";
    public const string Role = "role";
    public const string SecurityStamp = "security_stamp";
}
