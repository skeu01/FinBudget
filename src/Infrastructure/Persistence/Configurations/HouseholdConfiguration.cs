using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Configurations;

public sealed class HouseholdConfiguration : IEntityTypeConfiguration<Household>
{
    public void Configure(EntityTypeBuilder<Household> builder)
    {
        builder.ConfigureEntity("households");
        builder.Property(household => household.Name).HasMaxLength(120).IsRequired();
        builder.Property(household => household.Currency)
            .HasConversion(new ValueConverter<CurrencyCode, string>(
                currency => currency.Value,
                value => new CurrencyCode(value)))
            .HasMaxLength(3)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(household => household.TimeZone).HasMaxLength(64).IsRequired();
        builder.Property(household => household.CreatedByUserId).IsRequired();
        builder.Property(household => household.CreatedAt).IsRequired();
        builder.Property(household => household.ArchivedAt);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(household => household.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
