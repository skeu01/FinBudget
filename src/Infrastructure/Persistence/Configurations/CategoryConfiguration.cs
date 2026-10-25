using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ConfigureEntity("categories");
        builder.HasAlternateKey(category => new { category.HouseholdId, category.Id });
        builder.HasIndex(category => new { category.HouseholdId, category.Name, category.Kind }).IsUnique();
        builder.HasIndex(category => new { category.HouseholdId, category.ParentCategoryId });
        builder.Property(category => category.Name).HasMaxLength(80).IsRequired();
        builder.Property(category => category.Kind).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(category => category.IsActive).IsRequired();
        builder.Property(category => category.SortOrder).IsRequired();
        builder.Property(category => category.CreatedAt).IsRequired();
        builder.ToTable(table => table.HasCheckConstraint("ck_categories_not_own_parent", "\"ParentCategoryId\" IS NULL OR \"ParentCategoryId\" <> \"Id\""));
        builder.ConfigureHouseholdScope(nameof(Category.HouseholdId));
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(category => new { category.HouseholdId, category.ParentCategoryId })
            .HasPrincipalKey(category => new { category.HouseholdId, category.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
