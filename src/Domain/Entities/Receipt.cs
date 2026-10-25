using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public sealed class Receipt : Entity, IHouseholdScoped
{
    public Receipt(
        Guid householdId,
        Guid uploadedByMemberId,
        string storageKey,
        string originalFilename,
        string mediaType,
        long sizeBytes,
        string sha256)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(householdId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(uploadedByMemberId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFilename);
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeBytes);
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);

        HouseholdId = householdId;
        UploadedByMemberId = uploadedByMemberId;
        StorageKey = storageKey.Trim();
        OriginalFilename = originalFilename.Trim();
        MediaType = mediaType.Trim();
        SizeBytes = sizeBytes;
        Sha256 = sha256.Trim().ToLowerInvariant();
        Status = ReceiptStatus.Uploaded;
        UploadedAt = DateTimeOffset.UtcNow;
    }

    public Guid HouseholdId { get; }

    public Guid UploadedByMemberId { get; }

    public string StorageKey { get; }

    public string OriginalFilename { get; }

    public string MediaType { get; }

    public long SizeBytes { get; }

    public string Sha256 { get; }

    public ReceiptStatus Status { get; private set; }

    public Guid? ExpenseId { get; private set; }

    public Guid? IncomeId { get; private set; }

    public DateTimeOffset UploadedAt { get; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public void MarkProcessing()
    {
        EnsureNotDeletedOrConfirmed();
        Status = ReceiptStatus.Processing;
    }

    public void MarkNeedsReview()
    {
        EnsureNotDeletedOrConfirmed();
        Status = ReceiptStatus.NeedsReview;
    }

    public void MarkFailed()
    {
        EnsureNotDeletedOrConfirmed();
        Status = ReceiptStatus.Failed;
    }

    public void ConfirmForExpense(Guid expenseId)
    {
        EnsureCanBeConfirmed();
        ArgumentOutOfRangeException.ThrowIfEqual(expenseId, Guid.Empty);
        ExpenseId = expenseId;
        Status = ReceiptStatus.Confirmed;
    }

    public void ConfirmForIncome(Guid incomeId)
    {
        EnsureCanBeConfirmed();
        ArgumentOutOfRangeException.ThrowIfEqual(incomeId, Guid.Empty);
        IncomeId = incomeId;
        Status = ReceiptStatus.Confirmed;
    }

    public void Delete(DateTimeOffset deletedAt)
    {
        if (Status == ReceiptStatus.Deleted)
        {
            throw new InvalidOperationException("Receipt is already deleted.");
        }

        Status = ReceiptStatus.Deleted;
        DeletedAt = deletedAt;
    }

    private void EnsureCanBeConfirmed()
    {
        if (Status is ReceiptStatus.Confirmed or ReceiptStatus.Deleted)
        {
            throw new InvalidOperationException("This receipt cannot be confirmed.");
        }
    }

    private void EnsureNotDeletedOrConfirmed()
    {
        if (Status is ReceiptStatus.Confirmed or ReceiptStatus.Deleted)
        {
            throw new InvalidOperationException("Confirmed or deleted receipts cannot change processing state.");
        }
    }
}
