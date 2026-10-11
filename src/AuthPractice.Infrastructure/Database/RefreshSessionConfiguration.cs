using AuthPractice.Domain.Auth;
using AuthPractice.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthPractice.Infrastructure.Database;

// auth.refresh_sessions — серверные refresh-сессии (отдельная схема от таблиц Identity).
// Имена колонок/индексов — snake_case через UseSnakeCaseNamingConvention (token_hash, family_id, ...)
public sealed class RefreshSessionConfiguration : IEntityTypeConfiguration<RefreshSession>
{
    public const string Schema = "auth";

    public void Configure(EntityTypeBuilder<RefreshSession> builder)
    {
        builder.ToTable("refresh_sessions", Schema);

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        // SHA-256 в hex — ровно 64 символа. По нему ищем сессию на каждом refresh
        builder.Property(s => s.TokenHash).HasMaxLength(64).IsFixedLength();
        builder.HasIndex(s => s.TokenHash).IsUnique();

        builder.Property(s => s.RevokeReason).HasConversion<string>().HasMaxLength(32);

        // Отзыв семьи: все живые сессии одного входа
        builder.HasIndex(s => new { s.FamilyId, s.RevokedAt });

        builder.Ignore(s => s.IsRevoked);

        // Удалили пользователя — его сессии тоже
        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
