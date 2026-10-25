using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ConfigureEntity("receipts");
        builder.HasAlternateKey(receipt => new { receipt.HouseholdId, receipt.Id });
        builder.HasIndex(receipt => receipt.StorageKey).IsUnique();
        builder.HasIndex(receipt => new { receipt.HouseholdId, receipt.Status, receipt.UploadedAt });
        builder.HasIndex(receipt => receipt.Sha256);
        builder.HasIndex(receipt => receipt.ExpenseId).HasFilter("\"expense_id\" IS NOT NULL");
        builder.HasIndex(receipt => receipt.IncomeId).HasFilter("\"income_id\" IS NOT NULL");
        builder.Property(receipt => receipt.StorageKey).IsRequired();
        builder.Property(receipt => receipt.OriginalFilename).HasMaxLength(255).IsRequired();
        builder.Property(receipt => receipt.MediaType).HasMaxLength(100).IsRequired();
        builder.Property(receipt => receipt.SizeBytes).IsRequired();
        builder.Property(receipt => receipt.Sha256).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(receipt => receipt.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(receipt => receipt.UploadedAt).IsRequired();
        builder.Property(receipt => receipt.DeletedAt);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_receipts_size_positive", "\"size_bytes\" > 0");
            table.HasCheckConstraint("ck_receipts_single_transaction", "\"expense_id\" IS NULL OR \"income_id\" IS NULL");
        });
        builder.ConfigureHouseholdScope(nameof(Receipt.HouseholdId));
        builder.HasOne<HouseholdMember>()
            .WithMany()
            .HasForeignKey(receipt => new { receipt.HouseholdId, receipt.UploadedByMemberId })
            .HasPrincipalKey(member => new { member.HouseholdId, member.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Expense>()
            .WithMany()
            .HasForeignKey(receipt => new { receipt.HouseholdId, receipt.ExpenseId })
            .HasPrincipalKey(expense => new { expense.HouseholdId, expense.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Income>()
            .WithMany()
            .HasForeignKey(receipt => new { receipt.HouseholdId, receipt.IncomeId })
            .HasPrincipalKey(income => new { income.HouseholdId, income.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
