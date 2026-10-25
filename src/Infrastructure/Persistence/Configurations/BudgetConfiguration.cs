using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Configurations;

public sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ConfigureEntity("budgets");
        builder.HasIndex(budget => new { budget.HouseholdId, budget.CategoryId, budget.Period }).IsUnique();
        builder.Property(budget => budget.Period)
            .HasConversion(new ValueConverter<MonthPeriod, DateOnly>(
                period => period.StartDate,
                date => new MonthPeriod(date.Year, date.Month)))
            .HasColumnName("month")
            .HasColumnType("date")
            .IsRequired();
        builder.OwnsOne(budget => budget.AmountLimit, money => money.ConfigureMoney("limit"));
        builder.Navigation(budget => budget.AmountLimit).IsRequired();
        builder.OwnsOne(budget => budget.AlertThreshold, percentage =>
        {
            percentage.Property(value => value.Value)
                .HasColumnName("alert_threshold_percent")
                .HasPrecision(5, 2)
                .IsRequired();
        });
        builder.Navigation(budget => budget.AlertThreshold).IsRequired();
        builder.Property(budget => budget.CreatedAt).IsRequired();
        builder.Property(budget => budget.UpdatedAt);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_budgets_limit_positive", "\"limit_amount\" > 0");
            table.HasCheckConstraint("ck_budgets_month_first_day", "EXTRACT(DAY FROM \"month\") = 1");
            table.HasCheckConstraint("ck_budgets_alert_threshold_valid", "\"alert_threshold_percent\" > 0 AND \"alert_threshold_percent\" <= 100");
        });
        builder.ConfigureHouseholdScope(nameof(Budget.HouseholdId));
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(budget => new { budget.HouseholdId, budget.CategoryId })
            .HasPrincipalKey(category => new { category.HouseholdId, category.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<HouseholdMember>()
            .WithMany()
            .HasForeignKey(budget => new { budget.HouseholdId, budget.CreatedByMemberId })
            .HasPrincipalKey(member => new { member.HouseholdId, member.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
