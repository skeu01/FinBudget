using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class SavingsGoalConfiguration : IEntityTypeConfiguration<SavingsGoal>
{
    public void Configure(EntityTypeBuilder<SavingsGoal> builder)
    {
        builder.ConfigureEntity("savings_goals");
        builder.HasAlternateKey(goal => new { goal.HouseholdId, goal.Id });
        builder.HasIndex(goal => new { goal.HouseholdId, goal.Status });
        builder.Property(goal => goal.Name).HasMaxLength(120).IsRequired();
        builder.Property(goal => goal.Description);
        builder.Property(goal => goal.TargetDate);
        builder.Property(goal => goal.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(goal => goal.CreatedAt).IsRequired();
        builder.Property(goal => goal.CompletedAt);
        builder.OwnsOne(goal => goal.TargetAmount, money => money.ConfigureMoney("target"));
        builder.Navigation(goal => goal.TargetAmount).IsRequired();
        builder.ToTable(table => table.HasCheckConstraint("ck_savings_goals_target_positive", "\"target_amount\" > 0"));
        builder.ConfigureHouseholdScope(nameof(SavingsGoal.HouseholdId));
        builder.HasOne<HouseholdMember>()
            .WithMany()
            .HasForeignKey(goal => new { goal.HouseholdId, goal.CreatedByMemberId })
            .HasPrincipalKey(member => new { member.HouseholdId, member.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
