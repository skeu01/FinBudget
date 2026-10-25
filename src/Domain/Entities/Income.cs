using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class Income : Entity, IHouseholdScoped
{
    private Income()
    {
        Amount = null!;
    }

    public Income(
        Guid householdId,
        Guid categoryId,
        Guid createdByMemberId,
        Money amount,
        DateOnly occurredOn,
        string? description = null,
        string? source = null,
        string? notes = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(householdId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(categoryId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(createdByMemberId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(amount);
        if (amount.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Income amount must be greater than zero.");
        }

        HouseholdId = householdId;
        CategoryId = categoryId;
        CreatedByMemberId = createdByMemberId;
        Amount = amount;
        OccurredOn = occurredOn;
        Description = NormalizeOptional(description);
        Source = NormalizeOptional(source);
        Notes = NormalizeOptional(notes);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid HouseholdId { get; private set; }

    public Guid CategoryId { get; private set; }

    public Guid CreatedByMemberId { get; private set; }

    public Money Amount { get; private set; }

    public DateOnly OccurredOn { get; private set; }

    public string? Description { get; private set; }

    public string? Source { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt.HasValue;

    public void Update(
        Guid categoryId,
        Money amount,
        DateOnly occurredOn,
        string? description,
        string? source,
        string? notes)
    {
        EnsureNotDeleted();
        ArgumentOutOfRangeException.ThrowIfEqual(categoryId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(amount);
        if (amount.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Income amount must be greater than zero.");
        }

        CategoryId = categoryId;
        Amount = amount;
        OccurredOn = occurredOn;
        Description = NormalizeOptional(description);
        Source = NormalizeOptional(source);
        Notes = NormalizeOptional(notes);
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
            throw new InvalidOperationException("Deleted incomes cannot be changed.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
