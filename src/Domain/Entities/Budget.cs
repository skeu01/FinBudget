using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class Budget : Entity, IHouseholdScoped
{
    private Budget()
    {
        Period = null!;
        AmountLimit = null!;
        AlertThreshold = null!;
    }

    public Budget(
        Guid householdId,
        Guid categoryId,
        MonthPeriod period,
        Money amountLimit,
        Percentage alertThreshold,
        Guid createdByMemberId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(householdId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(categoryId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(createdByMemberId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(amountLimit);
        ArgumentNullException.ThrowIfNull(alertThreshold);
        if (amountLimit.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountLimit), "Budget limit must be greater than zero.");
        }

        HouseholdId = householdId;
        CategoryId = categoryId;
        Period = period;
        AmountLimit = amountLimit;
        AlertThreshold = alertThreshold;
        CreatedByMemberId = createdByMemberId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid HouseholdId { get; private set; }

    public Guid CategoryId { get; private set; }

    public MonthPeriod Period { get; private set; }

    public Money AmountLimit { get; private set; }

    public Percentage AlertThreshold { get; private set; }

    public Guid CreatedByMemberId { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public void Revise(Money amountLimit, Percentage alertThreshold)
    {
        ArgumentNullException.ThrowIfNull(amountLimit);
        ArgumentNullException.ThrowIfNull(alertThreshold);
        if (amountLimit.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountLimit), "Budget limit must be greater than zero.");
        }

        AmountLimit = amountLimit;
        AlertThreshold = alertThreshold;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsAlertThresholdReached(Money spent)
    {
        ArgumentNullException.ThrowIfNull(spent);
        if (spent.Currency != AmountLimit.Currency)
        {
            throw new InvalidOperationException("Spent amount and budget must use the same currency.");
        }

        return AlertThreshold.IsReachedBy(spent.Amount, AmountLimit.Amount);
    }

    public bool IsOverspent(Money spent)
    {
        ArgumentNullException.ThrowIfNull(spent);
        if (spent.Currency != AmountLimit.Currency)
        {
            throw new InvalidOperationException("Spent amount and budget must use the same currency.");
        }

        return spent.Amount > AmountLimit.Amount;
    }
}
