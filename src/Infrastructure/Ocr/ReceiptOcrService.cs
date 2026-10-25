using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Ocr;

public sealed class ReceiptOcrService(
    FinancialDbContext db,
    IReceiptFileStore fileStore,
    IReceiptOcrProvider provider,
    IOptions<ReceiptOcrOptions> options,
    ILogger<ReceiptOcrService> logger) : IReceiptOcrService
{
    private readonly ReceiptOcrOptions _options = options.Value;

    public async Task<ReceiptOcrSnapshot> UploadAndAnalyzeAsync(
        Guid userId,
        Guid householdId,
        Stream content,
        string originalFilename,
        string mediaType,
        long declaredSizeBytes,
        CancellationToken cancellationToken)
    {
        if (declaredSizeBytes <= 0 || declaredSizeBytes > _options.MaxFileSizeBytes)
        {
            throw new ArgumentException("The file must be non-empty and within the configured size limit.");
        }

        var filename = NormalizeFilename(originalFilename);
        var extension = Path.GetExtension(filename).ToLowerInvariant();
        var expectedMediaType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            _ => throw new ArgumentException("Only JPEG, PNG, and PDF files are supported.")
        };

        if (!string.Equals(mediaType, expectedMediaType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The file media type does not match its extension.");
        }

        var member = await db.HouseholdMembers.SingleOrDefaultAsync(
            item => item.HouseholdId == householdId && item.UserId == userId && item.Status == MembershipStatus.Active,
            cancellationToken)
            ?? throw new UnauthorizedAccessException("An active household membership is required.");

        var household = await db.Households.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == householdId, cancellationToken)
            ?? throw new KeyNotFoundException("Household not found.");

        var storedFile = await fileStore.StoreAsync(
            content, extension, _options.MaxFileSizeBytes, cancellationToken);

        var receipt = new Receipt(
            householdId, member.Id, storedFile.StorageKey, filename, expectedMediaType,
            storedFile.SizeBytes, storedFile.Sha256);
        var ocrResult = new OcrResult(receipt.Id, 1, provider.Name);
        receipt.MarkProcessing();
        ocrResult.MarkProcessing();

        db.Receipts.Add(receipt);
        db.OcrResults.Add(ocrResult);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            fileStore.Delete(storedFile.StorageKey);
            throw;
        }

        OcrDocumentText recognized;
        try
        {
            recognized = await provider.RecognizeAsync(
                storedFile.FullPath, expectedMediaType, cancellationToken);
            if (string.IsNullOrWhiteSpace(recognized.Text))
            {
                throw new OcrProviderException("no_text_detected", "No readable text was found in the uploaded document.");
            }
        }
        catch (OcrProviderException exception)
        {
            await MarkFailedAsync(receipt, ocrResult, exception.ErrorCode, cancellationToken);
            logger.LogWarning(exception,
                "Receipt OCR failed for receipt {ReceiptId} using provider {Provider} with code {ErrorCode}.",
                receipt.Id, provider.Name, exception.ErrorCode);
            return ToSnapshot(receipt, ocrResult, household.Currency.Value);
        }
        catch (OperationCanceledException)
        {
            await MarkFailedAsync(receipt, ocrResult, "processing_cancelled", CancellationToken.None);
            throw;
        }

        var extractedData = OcrTextParser.Parse(
            recognized.Text, household.Currency, recognized.Confidence);
        ocrResult.Complete(extractedData, DateTimeOffset.UtcNow);
        receipt.MarkNeedsReview();
        await db.SaveChangesAsync(cancellationToken);

        return ToSnapshot(receipt, ocrResult, household.Currency.Value);
    }

    public async Task<ReceiptOcrSnapshot> GetAsync(
        Guid userId,
        Guid householdId,
        Guid receiptId,
        CancellationToken cancellationToken)
    {
        var isMember = await db.HouseholdMembers.AsNoTracking().AnyAsync(
            member => member.HouseholdId == householdId &&
                      member.UserId == userId &&
                      member.Status == MembershipStatus.Active,
            cancellationToken);
        if (!isMember)
        {
            throw new UnauthorizedAccessException("An active household membership is required.");
        }

        var receipt = await db.Receipts.AsNoTracking().SingleOrDefaultAsync(
            item => item.HouseholdId == householdId &&
                    item.Id == receiptId &&
                    item.Status != ReceiptStatus.Deleted,
            cancellationToken)
            ?? throw new KeyNotFoundException("Receipt not found.");

        var result = await db.OcrResults.AsNoTracking()
            .Where(item => item.ReceiptId == receiptId)
            .OrderByDescending(item => item.AttemptNumber)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("OCR result not found.");

        var currency = result.ExtractedData?.Total?.Currency.Value ??
                       result.ExtractedData?.Vat?.Currency.Value;
        if (currency is null)
        {
            var household = await db.Households.AsNoTracking()
                .SingleAsync(item => item.Id == householdId, cancellationToken);
            currency = household.Currency.Value;
        }

        return ToSnapshot(receipt, result, currency);
    }

    private static ReceiptOcrSnapshot ToSnapshot(Receipt receipt, OcrResult result, string currency)
    {
        var data = result.ExtractedData;
        return new ReceiptOcrSnapshot(
            receipt.Id,
            receipt.HouseholdId,
            receipt.OriginalFilename,
            receipt.MediaType,
            receipt.SizeBytes,
            receipt.Status.ToString(),
            result.Provider,
            result.Status,
            result.AttemptNumber,
            data?.OccurredOn,
            data?.Merchant,
            data?.Total?.Amount,
            data?.Vat?.Amount,
            currency,
            data?.Confidence?.Value,
            result.ErrorCode,
            receipt.UploadedAt);
    }

    private async Task MarkFailedAsync(
        Receipt receipt,
        OcrResult result,
        string errorCode,
        CancellationToken cancellationToken)
    {
        result.Fail(errorCode, DateTimeOffset.UtcNow);
        receipt.MarkFailed();
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeFilename(string originalFilename)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFilename);
        var normalized = Path.GetFileName(originalFilename.Replace('\\', '/'));
        if (normalized.Length > 255)
        {
            throw new ArgumentException("The filename cannot exceed 255 characters.");
        }

        return normalized;
    }
}
