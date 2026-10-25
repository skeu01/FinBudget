using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Infrastructure.Ocr;

public sealed class LocalReceiptFileStore(IOptions<ReceiptOcrOptions> options) : IReceiptFileStore
{
    private readonly string _rootPath = Path.GetFullPath(options.Value.StoragePath);

    public async Task<StoredReceiptFile> StoreAsync(
        Stream content,
        string extension,
        long maxFileSizeBytes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFileSizeBytes);

        Directory.CreateDirectory(_rootPath);
        var storageKey = $"{Guid.NewGuid():N}{extension}";
        var fullPath = GetFullPath(storageKey);
        long sizeBytes = 0;
        var signature = new byte[8];
        var signatureLength = 0;

        try
        {
            await using var destination = new FileStream(
                fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[81920];

            while (true)
            {
                var read = await content.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    break;
                }

                sizeBytes = checked(sizeBytes + read);
                if (sizeBytes > maxFileSizeBytes)
                {
                    throw new ArgumentException("The uploaded file exceeds the configured size limit.");
                }

                var signatureBytes = Math.Min(read, signature.Length - signatureLength);
                if (signatureBytes > 0)
                {
                    buffer.AsSpan(0, signatureBytes).CopyTo(signature.AsSpan(signatureLength));
                    signatureLength += signatureBytes;
                }

                hash.AppendData(buffer, 0, read);
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            if (sizeBytes == 0 || !HasExpectedSignature(extension, signature, signatureLength))
            {
                throw new ArgumentException("The file content does not match its supported image or PDF format.");
            }

            await destination.FlushAsync(cancellationToken);
            return new StoredReceiptFile(storageKey, fullPath, sizeBytes, Convert.ToHexString(hash.GetHashAndReset()));
        }
        catch
        {
            Delete(storageKey);
            throw;
        }
    }

    public string GetFullPath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) ||
            !string.Equals(Path.GetFileName(storageKey), storageKey, StringComparison.Ordinal) ||
            !Guid.TryParseExact(Path.GetFileNameWithoutExtension(storageKey), "N", out _) ||
            Path.GetExtension(storageKey) is not (".jpg" or ".jpeg" or ".png" or ".pdf"))
        {
            throw new InvalidOperationException("The receipt storage key is invalid.");
        }

        return Path.Combine(_rootPath, storageKey);
    }

    public void Delete(string storageKey)
    {
        var fullPath = GetFullPath(storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private static bool HasExpectedSignature(string extension, byte[] signature, int length) =>
        extension switch
        {
            ".jpg" or ".jpeg" => length >= 3 && signature[0] == 0xFF && signature[1] == 0xD8 && signature[2] == 0xFF,
            ".png" => length >= 8 &&
                      signature.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".pdf" => length >= 5 && signature.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            _ => false
        };
}
