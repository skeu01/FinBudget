using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public sealed class Category : Entity, IHouseholdScoped
{
    public Category(
        Guid householdId,
        string name,
        CategoryKind kind,
        Guid? parentCategoryId = null,
        int sortOrder = 0)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(householdId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (parentCategoryId == Id)
        {
            throw new ArgumentException("A category cannot be its own parent.", nameof(parentCategoryId));
        }

        HouseholdId = householdId;
        Name = name.Trim();
        Kind = kind;
        ParentCategoryId = parentCategoryId;
        SortOrder = sortOrder;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid HouseholdId { get; }

    public Guid? ParentCategoryId { get; private set; }

    public string Name { get; private set; }

    public CategoryKind Kind { get; }

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public void Rename(string name)
    {
        EnsureActive();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public void Reorder(int sortOrder)
    {
        EnsureActive();
        SortOrder = sortOrder;
    }

    public void SetParent(Guid? parentCategoryId)
    {
        EnsureActive();
        if (parentCategoryId == Id)
        {
            throw new ArgumentException("A category cannot be its own parent.", nameof(parentCategoryId));
        }

        ParentCategoryId = parentCategoryId;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Inactive categories cannot be changed.");
        }
    }
}
