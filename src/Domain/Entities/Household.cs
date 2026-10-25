using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class Household : Entity, IAggregateRoot
{
    public Household(
        string name,
        CurrencyCode currency,
        string timeZone,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZone);
        ArgumentOutOfRangeException.ThrowIfEqual(createdByUserId, Guid.Empty);

        Name = name.Trim();
        Currency = currency;
        TimeZone = timeZone.Trim();
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Name { get; private set; }

    public CurrencyCode Currency { get; private set; }

    public string TimeZone { get; private set; }

    public Guid CreatedByUserId { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public bool IsArchived => ArchivedAt.HasValue;

    public void Rename(string name)
    {
        EnsureActive();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public void ChangeCurrency(CurrencyCode currency)
    {
        EnsureActive();
        ArgumentNullException.ThrowIfNull(currency);
        Currency = currency;
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (IsArchived)
        {
            throw new InvalidOperationException("Household is already archived.");
        }

        ArchivedAt = archivedAt;
    }

    private void EnsureActive()
    {
        if (IsArchived)
        {
            throw new InvalidOperationException("Archived households cannot be changed.");
        }
    }
}
