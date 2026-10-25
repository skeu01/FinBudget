using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ConfigureEntity("refresh_tokens");
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.UserId, token.ExpiresAt });
        builder.HasIndex(token => new { token.FamilyId, token.RevokedAt });
        builder.Property(token => token.FamilyId).IsRequired();
        builder.Property(token => token.TokenHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(token => token.UserId).IsRequired();
        builder.Property(token => token.CreatedAt).IsRequired();
        builder.Property(token => token.ExpiresAt).IsRequired();
        builder.Property(token => token.RevokedAt);
        builder.Property(token => token.RevokedAt).IsConcurrencyToken();
        builder.Property(token => token.ReplacedByTokenHash).HasMaxLength(64).IsFixedLength();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.ToTable(table => table.HasCheckConstraint(
            "ck_refresh_tokens_expiry_after_creation",
            "\"expires_at\" > \"created_at\""));
    }
}
