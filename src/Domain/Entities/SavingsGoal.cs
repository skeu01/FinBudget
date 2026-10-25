using Domain.Common;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class SavingsGoal : Entity, IHouseholdScoped
{
    private SavingsGoal()
    {
        Name = null!;
        TargetAmount = null!;
    }

    public SavingsGoal(
        Guid householdId,
        string name,
        Money targetAmount,
        Guid createdByMemberId,
        DateOnly? targetDate = null,
        string? description = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(householdId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(targetAmount);
        ArgumentOutOfRangeException.ThrowIfEqual(createdByMemberId, Guid.Empty);
        if (targetAmount.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetAmount), "Goal target must be greater than zero.");
        }

        HouseholdId = householdId;
        Name = name.Trim();
        TargetAmount = targetAmount;
        CreatedByMemberId = createdByMemberId;
        TargetDate = targetDate;
        Description = NormalizeOptional(description);
        Status = SavingsGoalStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid HouseholdId { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public Money TargetAmount { get; private set; }

    public Guid CreatedByMemberId { get; private set; }

    public DateOnly? TargetDate { get; private set; }

    public SavingsGoalStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public void Revise(string name, string? description, Money targetAmount, DateOnly? targetDate)
    {
        EnsureActive();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(targetAmount);
        if (targetAmount.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetAmount), "Goal target must be greater than zero.");
        }

        Name = name.Trim();
        Description = NormalizeOptional(description);
        TargetAmount = targetAmount;
        TargetDate = targetDate;
    }

    public void Complete(DateTimeOffset completedAt)
    {
        EnsureActive();
        Status = SavingsGoalStatus.Completed;
        CompletedAt = completedAt;
    }

    public void Cancel()
    {
        EnsureActive();
        Status = SavingsGoalStatus.Cancelled;
    }

    private void EnsureActive()
    {
        if (Status != SavingsGoalStatus.Active)
        {
            throw new InvalidOperationException("Only active savings goals can be changed.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
