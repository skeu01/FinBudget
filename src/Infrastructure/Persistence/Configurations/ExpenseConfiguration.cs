using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ConfigureEntity("expenses");
        builder.HasAlternateKey(expense => new { expense.HouseholdId, expense.Id });
        builder.HasIndex(expense => new { expense.HouseholdId, expense.OccurredOn });
        builder.HasIndex(expense => new { expense.HouseholdId, expense.CategoryId, expense.OccurredOn });
        builder.HasIndex(expense => new { expense.HouseholdId, expense.CreatedByMemberId, expense.OccurredOn });
        builder.Property(expense => expense.OccurredOn).IsRequired();
        builder.Property(expense => expense.Description).HasMaxLength(200);
        builder.Property(expense => expense.Merchant).HasMaxLength(160);
        builder.Property(expense => expense.Notes);
        builder.Property(expense => expense.CreatedAt).IsRequired();
        builder.Property(expense => expense.UpdatedAt);
        builder.Property(expense => expense.DeletedAt);
        builder.OwnsOne(expense => expense.Amount, money => money.ConfigureMoney("amount"));
        builder.Navigation(expense => expense.Amount).IsRequired();
        builder.ToTable(table => table.HasCheckConstraint("ck_expenses_amount_positive", "\"amount_amount\" > 0"));
        builder.ConfigureHouseholdScope(nameof(Expense.HouseholdId));
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(expense => new { expense.HouseholdId, expense.CategoryId })
            .HasPrincipalKey(category => new { category.HouseholdId, category.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<HouseholdMember>()
            .WithMany()
            .HasForeignKey(expense => new { expense.HouseholdId, expense.CreatedByMemberId })
            .HasPrincipalKey(member => new { member.HouseholdId, member.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
