using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class IncomeConfiguration : IEntityTypeConfiguration<Income>
{
    public void Configure(EntityTypeBuilder<Income> builder)
    {
        builder.ConfigureEntity("incomes");
        builder.HasAlternateKey(income => new { income.HouseholdId, income.Id });
        builder.HasIndex(income => new { income.HouseholdId, income.OccurredOn });
        builder.HasIndex(income => new { income.HouseholdId, income.CategoryId, income.OccurredOn });
        builder.Property(income => income.OccurredOn).IsRequired();
        builder.Property(income => income.Description).HasMaxLength(200);
        builder.Property(income => income.Source).HasMaxLength(160);
        builder.Property(income => income.Notes);
        builder.Property(income => income.CreatedAt).IsRequired();
        builder.Property(income => income.UpdatedAt);
        builder.Property(income => income.DeletedAt);
        builder.OwnsOne(income => income.Amount, money => money.ConfigureMoney("amount"));
        builder.Navigation(income => income.Amount).IsRequired();
        builder.ToTable(table => table.HasCheckConstraint("ck_incomes_amount_positive", "\"amount_amount\" > 0"));
        builder.ConfigureHouseholdScope(nameof(Income.HouseholdId));
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(income => new { income.HouseholdId, income.CategoryId })
            .HasPrincipalKey(category => new { category.HouseholdId, category.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<HouseholdMember>()
            .WithMany()
            .HasForeignKey(income => new { income.HouseholdId, income.CreatedByMemberId })
            .HasPrincipalKey(member => new { member.HouseholdId, member.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
