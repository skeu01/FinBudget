using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ConfigureEntity("users");
        builder.Property(user => user.Email)
            .HasConversion(new ValueConverter<Domain.ValueObjects.EmailAddress, string>(
                email => email.Value,
                value => new Domain.ValueObjects.EmailAddress(value)))
            .HasMaxLength(320)
            .IsRequired();
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Property(user => user.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(user => user.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();
        builder.Property(user => user.EmailVerifiedAt);
    }
}
