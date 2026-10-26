using Domain.Enums;
using Domain.ValueObjects;

namespace Infrastructure.Ocr;

public interface IReceiptOcrService
{
    Task<ReceiptOcrSnapshot> UploadAndAnalyzeAsync(
        Guid userId,
        Guid householdId,
        Stream content,
        string originalFilename,
        string mediaType,
        long declaredSizeBytes,
        CancellationToken cancellationToken);

    Task<ReceiptOcrSnapshot> GetAsync(
        Guid userId,
        Guid householdId,
        Guid receiptId,
        CancellationToken cancellationToken);
}

public sealed record ReceiptOcrSnapshot(
    Guid Id,
    Guid HouseholdId,
    string OriginalFilename,
    string MediaType,
    long SizeBytes,
    string ReceiptStatus,
    string Provider,
    OcrStatus OcrStatus,
    int AttemptNumber,
    DateOnly? OccurredOn,
    string? Merchant,
    IReadOnlyList<ExpenseItem> Items,
    decimal? Total,
    decimal? Vat,
    string Currency,
    decimal? Confidence,
    string? ErrorCode,
    DateTimeOffset UploadedAt);
