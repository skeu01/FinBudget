using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class Expense : Entity, IHouseholdScoped
{
    private Expense()
    {
        Amount = null!;
    }

    public Expense(
        Guid householdId,
        Guid categoryId,
        Guid createdByMemberId,
        Money amount,
        DateOnly occurredOn,
        string? description = null,
        string? merchant = null,
        string? notes = null,
        IReadOnlyList<ExpenseItem>? items = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(householdId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(categoryId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(createdByMemberId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(amount);
        if (amount.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Expense amount must be greater than zero.");
        }

        HouseholdId = householdId;
        CategoryId = categoryId;
        CreatedByMemberId = createdByMemberId;
        Amount = amount;
        OccurredOn = occurredOn;
        Description = NormalizeOptional(description);
        Merchant = NormalizeOptional(merchant);
        Notes = NormalizeOptional(notes);
        Items = NormalizeItems(items);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid HouseholdId { get; private set; }

    public Guid CategoryId { get; private set; }

    public Guid CreatedByMemberId { get; private set; }

    public Money Amount { get; private set; }

    public DateOnly OccurredOn { get; private set; }

    public string? Description { get; private set; }

    public string? Merchant { get; private set; }

    public string? Notes { get; private set; }

    public List<ExpenseItem>? Items { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt.HasValue;

    public void Update(
        Guid categoryId,
        Money amount,
        DateOnly occurredOn,
        string? description,
        string? merchant,
        string? notes,
        IReadOnlyList<ExpenseItem>? items = null)
    {
        EnsureNotDeleted();
        ArgumentOutOfRangeException.ThrowIfEqual(categoryId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(amount);
        if (amount.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Expense amount must be greater than zero.");
        }

        CategoryId = categoryId;
        Amount = amount;
        OccurredOn = occurredOn;
        Description = NormalizeOptional(description);
        Merchant = NormalizeOptional(merchant);
        Notes = NormalizeOptional(notes);
        Items = NormalizeItems(items);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Delete(DateTimeOffset deletedAt)
    {
        EnsureNotDeleted();
        DeletedAt = deletedAt;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Deleted expenses cannot be changed.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<ExpenseItem>? NormalizeItems(IReadOnlyList<ExpenseItem>? items)
    {
        if (items is null || items.Count == 0)
        {
            return null;
        }

        if (items.Count > 500)
        {
            throw new ArgumentException("An expense cannot contain more than 500 items.", nameof(items));
        }

        var normalized = new List<ExpenseItem>(items.Count);
        foreach (var item in items)
        {
            ArgumentNullException.ThrowIfNull(item);
            var description = item.Description.Trim();
            if (description.Length is 0 or > 200)
            {
                throw new ArgumentException("Each expense item must have a description between 1 and 200 characters.", nameof(items));
            }
            if (item.Quantity <= 0 || item.Quantity > 999999999999.99m)
            {
                throw new ArgumentOutOfRangeException(nameof(items), "Each expense item quantity must be positive and within the supported range.");
            }
            if (item.Price <= 0 || item.Price > 999999999999.99m)
            {
                throw new ArgumentOutOfRangeException(nameof(items), "Each expense item price must be positive and within the supported range.");
            }

            normalized.Add(item with { Description = description });
        }

        return normalized;
    }
}
