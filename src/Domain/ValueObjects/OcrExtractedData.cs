namespace Domain.ValueObjects;

public sealed record OcrExtractedData
{
    private OcrExtractedData()
    {
    }

    public OcrExtractedData(
        string? merchant,
        DateOnly? occurredOn,
        Money? total,
        Percentage? confidence,
        Money? vat = null)
    {
        if (total is not null && vat is not null && total.Currency != vat.Currency)
        {
            throw new ArgumentException("Receipt total and VAT must use the same currency.", nameof(vat));
        }

        Merchant = string.IsNullOrWhiteSpace(merchant) ? null : merchant.Trim();
        OccurredOn = occurredOn;
        Total = total;
        Confidence = confidence;
        Vat = vat;
    }

    public string? Merchant { get; private set; }

    public DateOnly? OccurredOn { get; private set; }

    public Money? Total { get; private set; }

    public Percentage? Confidence { get; private set; }

    public Money? Vat { get; private set; }
}
