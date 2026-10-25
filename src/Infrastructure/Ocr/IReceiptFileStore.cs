namespace Infrastructure.Ocr;

public interface IReceiptFileStore
{
    Task<StoredReceiptFile> StoreAsync(
        Stream content,
        string extension,
        long maxFileSizeBytes,
        CancellationToken cancellationToken);

    string GetFullPath(string storageKey);

    void Delete(string storageKey);
}

public sealed record StoredReceiptFile(string StorageKey, string FullPath, long SizeBytes, string Sha256);
