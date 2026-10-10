using AuthPractice.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AuthPractice.Infrastructure.Database;

// Таблицы Identity (users, roles, user_roles, ...) в схеме "identity"
public sealed class AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    public const string Schema = "identity";

    // Фиксированные Id/ConcurrencyStamp — иначе HasData генерировал бы новую миграцию каждый раз
    private static readonly IdentityRole<Guid>[] SeedRoles =
    [
        new()
        {
            Id = Guid.Parse("0199b5a0-0000-7000-8000-000000000001"),
            Name = Domain.Users.UserRoles.Admin,
            NormalizedName = Domain.Users.UserRoles.Admin.ToUpperInvariant(),
            ConcurrencyStamp = "0199b5a0-0000-7000-8000-000000000001",
        },
        new()
        {
            Id = Guid.Parse("0199b5a0-0000-7000-8000-000000000002"),
            Name = Domain.Users.UserRoles.User,
            NormalizedName = Domain.Users.UserRoles.User.ToUpperInvariant(),
            ConcurrencyStamp = "0199b5a0-0000-7000-8000-000000000002",
        },
    ];

    // EF 9: MigrateAsync бросает исключение, если модель разошлась с последней миграцией.
    // Не валим старт — просто забыли сделать migrations add
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema(Schema);

        builder.Entity<User>().ToTable("users");
        builder.Entity<IdentityRole<Guid>>().ToTable("roles").HasData(SeedRoles);
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
    }
}
