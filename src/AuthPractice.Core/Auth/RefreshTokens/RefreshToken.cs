using System.Security.Cryptography;
using System.Text;

namespace AuthPractice.Core.Auth.RefreshTokens;

// Сырой refresh-токен: 256 бит случайности в base64url. Отдаётся клиенту один раз (в HttpOnly-cookie)
public static class RefreshToken
{
    private const int SizeBytes = 32;

    public static string Generate() =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(SizeBytes));

    // SHA-256, а не bcrypt/PBKDF2: у случайных 256 бит перебирать нечего, медленный хэш тут
    // только тормозил бы каждый refresh. Результат — 64 hex-символа, это и лежит в БД
    public static string Hash(string rawToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
