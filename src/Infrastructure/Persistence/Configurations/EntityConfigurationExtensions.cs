using Domain.Common;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal static class EntityConfigurationExtensions
{
    public static void ConfigureEntity<TEntity>(this EntityTypeBuilder<TEntity> builder, string tableName)
        where TEntity : Entity
    {
        builder.ToTable(tableName);
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
    }

    public static void ConfigureMoney<TOwner>(
        this OwnedNavigationBuilder<TOwner, Money> builder,
        string columnPrefix)
        where TOwner : class
    {
        builder.Property(money => money.Amount)
            .HasColumnName($"{columnPrefix}_amount")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.OwnsOne(money => money.Currency, currency =>
        {
            currency.Property(code => code.Value)
                .HasColumnName($"{columnPrefix}_currency")
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsRequired();
        });

        builder.Navigation(money => money.Currency).IsRequired();
    }

    public static void ConfigureHouseholdScope<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string principalKeyName)
        where TEntity : class, IHouseholdScoped
    {
        builder.HasOne<Domain.Entities.Household>()
            .WithMany()
            .HasForeignKey(entity => entity.HouseholdId)
            .HasPrincipalKey(household => household.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
