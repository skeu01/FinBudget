using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class SavingsGoalContributionConfiguration : IEntityTypeConfiguration<SavingsGoalContribution>
{
    public void Configure(EntityTypeBuilder<SavingsGoalContribution> builder)
    {
        builder.ConfigureEntity("savings_goal_contributions");
        builder.HasIndex(contribution => new { contribution.HouseholdId, contribution.SavingsGoalId, contribution.ContributedOn });
        builder.Property(contribution => contribution.ContributedOn).IsRequired();
        builder.Property(contribution => contribution.Notes);
        builder.Property(contribution => contribution.CreatedAt).IsRequired();
        builder.OwnsOne(contribution => contribution.Amount, money => money.ConfigureMoney("amount"));
        builder.Navigation(contribution => contribution.Amount).IsRequired();
        builder.ToTable(table => table.HasCheckConstraint("ck_savings_contributions_amount_positive", "\"amount_amount\" > 0"));
        builder.ConfigureHouseholdScope(nameof(SavingsGoalContribution.HouseholdId));
        builder.HasOne<SavingsGoal>()
            .WithMany()
            .HasForeignKey(contribution => new { contribution.HouseholdId, contribution.SavingsGoalId })
            .HasPrincipalKey(goal => new { goal.HouseholdId, goal.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<HouseholdMember>()
            .WithMany()
            .HasForeignKey(contribution => new { contribution.HouseholdId, contribution.CreatedByMemberId })
            .HasPrincipalKey(member => new { member.HouseholdId, member.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
