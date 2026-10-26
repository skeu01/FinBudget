using Domain.Enums;

namespace Presentation.Contracts.Responses;

/// <summary>Receipt metadata and the fields suggested by OCR for user review.</summary>
public sealed record ReceiptOcrResponse(
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
    IReadOnlyList<Domain.ValueObjects.ExpenseItem> Items,
    decimal? Total,
    decimal? Vat,
    string Currency,
    decimal? Confidence,
    string? ErrorCode,
    DateTimeOffset UploadedAt);
