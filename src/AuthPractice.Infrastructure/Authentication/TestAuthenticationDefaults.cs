namespace AuthPractice.Infrastructure.Authentication;

public static class TestAuthenticationDefaults
{
    // Имя схемы — по нему ASP.NET находит наш handler
    public const string SchemeName = "TestScheme";

    // Секция в appsettings.json, из которой биндятся Options
    public const string ConfigSection = "TestAuthentication";
}
