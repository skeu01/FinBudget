using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public sealed class HouseholdMember : Entity, IHouseholdScoped
{
    public HouseholdMember(Guid householdId, Guid userId, HouseholdRole role)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(householdId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);

        HouseholdId = householdId;
        UserId = userId;
        Role = role;
        Status = MembershipStatus.Active;
        JoinedAt = DateTimeOffset.UtcNow;
    }

    public Guid HouseholdId { get; }

    public Guid UserId { get; }

    public HouseholdRole Role { get; private set; }

    public MembershipStatus Status { get; private set; }

    public DateTimeOffset JoinedAt { get; }

    public DateTimeOffset? RemovedAt { get; private set; }

    public void ChangeRole(HouseholdRole role)
    {
        EnsureActive();
        Role = role;
    }

    public void Remove(DateTimeOffset removedAt)
    {
        EnsureActive();
        Status = MembershipStatus.Removed;
        RemovedAt = removedAt;
    }

    private void EnsureActive()
    {
        if (Status != MembershipStatus.Active)
        {
            throw new InvalidOperationException("Only active household members can be changed.");
        }
    }
}
