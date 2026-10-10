using Microsoft.AspNetCore.Identity;

namespace AuthPractice.Domain.Users;

// Пользователь = IdentityUser (Email, UserName, PasswordHash, SecurityStamp, ...) с Guid-ключом
public sealed class User : IdentityUser<Guid>
{
    // Для EF
    private User()
    {
    }

    public static User Create(string userName, string email) => new()
    {
        Id = Guid.CreateVersion7(),
        UserName = userName,
        Email = email,
    };
}
