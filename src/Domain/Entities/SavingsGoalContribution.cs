using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class SavingsGoalContribution : Entity, IHouseholdScoped
{
    private SavingsGoalContribution()
    {
        Amount = null!;
    }

    public SavingsGoalContribution(
        Guid householdId,
        Guid savingsGoalId,
        Guid createdByMemberId,
        Money amount,
        DateOnly contributedOn,
        string? notes = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(householdId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(savingsGoalId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(createdByMemberId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(amount);
        if (amount.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Contribution must be greater than zero.");
        }

        HouseholdId = householdId;
        SavingsGoalId = savingsGoalId;
        CreatedByMemberId = createdByMemberId;
        Amount = amount;
        ContributedOn = contributedOn;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid HouseholdId { get; private set; }

    public Guid SavingsGoalId { get; private set; }

    public Guid CreatedByMemberId { get; private set; }

    public Money Amount { get; private set; }

    public DateOnly ContributedOn { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
