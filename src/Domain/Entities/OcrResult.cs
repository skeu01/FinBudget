using Domain.Common;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class OcrResult : Entity
{
    private OcrResult()
    {
        Provider = null!;
    }

    public OcrResult(Guid receiptId, int attemptNumber, string provider)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(receiptId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attemptNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        ReceiptId = receiptId;
        AttemptNumber = attemptNumber;
        Provider = provider.Trim();
        Status = OcrStatus.Queued;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public Guid ReceiptId { get; private set; }

    public int AttemptNumber { get; private set; }

    public string Provider { get; private set; }

    public OcrStatus Status { get; private set; }

    public OcrExtractedData? ExtractedData { get; private set; }

    public string? ErrorCode { get; private set; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public void MarkProcessing()
    {
        EnsureQueued();
        Status = OcrStatus.Processing;
    }

    public void Complete(OcrExtractedData extractedData, DateTimeOffset completedAt)
    {
        ArgumentNullException.ThrowIfNull(extractedData);
        if (Status is not (OcrStatus.Queued or OcrStatus.Processing))
        {
            throw new InvalidOperationException("Only queued or processing OCR attempts can complete.");
        }

        ExtractedData = extractedData;
        Status = OcrStatus.Succeeded;
        CompletedAt = completedAt;
    }

    public void Fail(string errorCode, DateTimeOffset completedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        if (Status is not (OcrStatus.Queued or OcrStatus.Processing))
        {
            throw new InvalidOperationException("Only queued or processing OCR attempts can fail.");
        }

        ErrorCode = errorCode.Trim();
        Status = OcrStatus.Failed;
        CompletedAt = completedAt;
    }

    private void EnsureQueued()
    {
        if (Status != OcrStatus.Queued)
        {
            throw new InvalidOperationException("Only queued OCR attempts can start processing.");
        }
    }
}
