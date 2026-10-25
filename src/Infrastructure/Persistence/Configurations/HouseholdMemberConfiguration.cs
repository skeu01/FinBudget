using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class HouseholdMemberConfiguration : IEntityTypeConfiguration<HouseholdMember>
{
    public void Configure(EntityTypeBuilder<HouseholdMember> builder)
    {
        builder.ConfigureEntity("household_members");
        builder.HasAlternateKey(member => new { member.HouseholdId, member.Id });
        builder.HasIndex(member => new { member.HouseholdId, member.UserId }).IsUnique();
        builder.HasIndex(member => new { member.UserId, member.Status });
        builder.HasIndex(member => new { member.HouseholdId, member.Status, member.Role });
        builder.Property(member => member.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(member => member.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(member => member.JoinedAt).IsRequired();
        builder.Property(member => member.RemovedAt);
        builder.ConfigureHouseholdScope(nameof(HouseholdMember.HouseholdId));
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
