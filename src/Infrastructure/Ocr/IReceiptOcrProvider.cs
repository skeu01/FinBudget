namespace Infrastructure.Ocr;

public interface IReceiptOcrProvider
{
    string Name { get; }

    Task<OcrDocumentText> RecognizeAsync(
        string filePath,
        string mediaType,
        CancellationToken cancellationToken);
}

public sealed record OcrDocumentText(string Text, decimal? Confidence);

public sealed class OcrProviderException(string errorCode, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string ErrorCode { get; } = errorCode;
}
