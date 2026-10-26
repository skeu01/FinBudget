using Domain.Entities;
using Domain.ValueObjects;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    private static readonly JsonSerializerOptions ExpenseItemJsonOptions = new()
    {
        Converters = { new ExpenseItemJsonConverter() }
    };

    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ConfigureEntity("expenses");
        builder.HasAlternateKey(expense => new { expense.HouseholdId, expense.Id });
        builder.HasIndex(expense => new { expense.HouseholdId, expense.OccurredOn });
        builder.HasIndex(expense => new { expense.HouseholdId, expense.CategoryId, expense.OccurredOn });
        builder.HasIndex(expense => new { expense.HouseholdId, expense.CreatedByMemberId, expense.OccurredOn });
        builder.Property(expense => expense.OccurredOn).IsRequired();
        builder.Property(expense => expense.Description).HasMaxLength(2000);
        builder.Property(expense => expense.Items)
            .HasConversion(
                items => items == null ? null : JsonSerializer.Serialize(items, ExpenseItemJsonOptions),
                json => json == null ? null : JsonSerializer.Deserialize<List<ExpenseItem>>(json, ExpenseItemJsonOptions),
                new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<ExpenseItem>?>(
                    (left, right) => ReferenceEquals(left, right) ||
                                     left != null && right != null && left.SequenceEqual(right),
                    items => items == null
                        ? 0
                        : items.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    items => items == null ? null : items.ToList()))
            .HasColumnType("jsonb");
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

    private sealed class ExpenseItemJsonConverter : JsonConverter<ExpenseItem>
    {
        public override ExpenseItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var item = document.RootElement;
            var description = GetProperty(item, "Description", legacyName: "Concept").GetString()
                ?? throw new JsonException("An expense item description is required.");
            var quantityProperty = GetProperty(item, "Quantity", required: false);
            var quantity = quantityProperty.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
                ? 1m
                : quantityProperty.GetDecimal();
            var price = GetProperty(item, "Price").GetDecimal();
            return new ExpenseItem(description, quantity, price);
        }

        public override void Write(Utf8JsonWriter writer, ExpenseItem value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Description", value.Description);
            writer.WriteNumber("Quantity", value.Quantity);
            writer.WriteNumber("Price", value.Price);
            writer.WriteEndObject();
        }

        private static JsonElement GetProperty(
            JsonElement item,
            string propertyName,
            bool required = true,
            string? legacyName = null)
        {
            foreach (var property in item.EnumerateObject())
            {
                if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase) ||
                    legacyName is not null && property.Name.Equals(legacyName, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }

            if (!required)
            {
                return default;
            }
            throw new JsonException($"An expense item {propertyName.ToLowerInvariant()} is required.");
        }
    }
}
