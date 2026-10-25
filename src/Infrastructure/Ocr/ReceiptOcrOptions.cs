namespace Infrastructure.Ocr;

public sealed class ReceiptOcrOptions
{
    public const string SectionName = "ReceiptOcr";

    public string Provider { get; set; } = "Tesseract";

    public string StoragePath { get; set; } = "App_Data/receipts";

    public long MaxFileSizeBytes { get; set; } = 15 * 1024 * 1024;

    public int MaxPdfPages { get; set; } = 20;

    public int ProcessTimeoutSeconds { get; set; } = 90;

    public string TesseractExecutable { get; set; } = "tesseract";

    public string PdfToPpmExecutable { get; set; } = "pdftoppm";

    public string TesseractLanguage { get; set; } = "spa+eng";

    public string? AzureEndpoint { get; set; }

    public string? AzureApiKey { get; set; }
}
