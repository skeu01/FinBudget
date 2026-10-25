using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;

namespace Infrastructure.Ocr;

public sealed class TesseractOcrProvider(IOptions<ReceiptOcrOptions> options) : IReceiptOcrProvider
{
    private readonly ReceiptOcrOptions _options = options.Value;

    public string Name => "Tesseract";

    public async Task<OcrDocumentText> RecognizeAsync(
        string filePath,
        string mediaType,
        CancellationToken cancellationToken)
    {
        if (mediaType == "application/pdf")
        {
            return await RecognizePdfAsync(filePath, cancellationToken);
        }

        return await RecognizeImageAsync(filePath, cancellationToken);
    }

    private async Task<OcrDocumentText> RecognizePdfAsync(string filePath, CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"receipt-ocr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        try
        {
            var prefix = Path.Combine(tempDirectory, "page");
            await RunProcessAsync(_options.PdfToPpmExecutable,
                ["-f", "1", "-l", (_options.MaxPdfPages + 1).ToString(CultureInfo.InvariantCulture),
                 "-r", "200", "-png", filePath, prefix],
                cancellationToken);

            var pages = Directory.GetFiles(tempDirectory, "page-*.png")
                .OrderBy(ParsePageNumber)
                .ToArray();
            if (pages.Length == 0)
            {
                throw new OcrProviderException("invalid_document", "The PDF does not contain readable pages.");
            }

            if (pages.Length > _options.MaxPdfPages)
            {
                throw new OcrProviderException("too_many_pages", "The PDF exceeds the maximum supported page count.");
            }

            var results = new List<OcrDocumentText>(pages.Length);
            foreach (var page in pages)
            {
                results.Add(await RecognizeImageAsync(page, cancellationToken));
            }

            var confidences = results.Where(result => result.Confidence.HasValue)
                .Select(result => result.Confidence!.Value)
                .ToArray();
            return new OcrDocumentText(
                string.Join(Environment.NewLine, results.Select(result => result.Text)),
                confidences.Length == 0 ? null : confidences.Average());
        }
        catch (OcrProviderException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new OcrProviderException("processing_failed", "The PDF could not be processed.", exception);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private async Task<OcrDocumentText> RecognizeImageAsync(string filePath, CancellationToken cancellationToken)
    {
        var output = await RunProcessAsync(_options.TesseractExecutable,
            [filePath, "stdout", "-l", _options.TesseractLanguage, "--psm", "6", "tsv"],
            cancellationToken);
        return ParseTsv(output);
    }

    private async Task<string> RunProcessAsync(
        string executable,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                throw new OcrProviderException("provider_unavailable", "The configured OCR tool could not be started.");
            }
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            throw new OcrProviderException("provider_unavailable", "The configured OCR tool is unavailable.", exception);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.ProcessTimeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            KillProcess(process);
            throw new OcrProviderException("processing_timeout", "OCR processing exceeded the configured time limit.");
        }
        catch
        {
            KillProcess(process);
            throw;
        }

        var output = await stdoutTask;
        var error = await stderrTask;
        if (process.ExitCode != 0)
        {
            throw new OcrProviderException(
                "processing_failed",
                $"The OCR tool failed with exit code {process.ExitCode}: {TrimForLog(error)}");
        }

        return output;
    }

    private static OcrDocumentText ParseTsv(string tsv)
    {
        var text = new StringBuilder();
        decimal totalConfidence = 0;
        var recognizedWords = 0;

        foreach (var line in tsv.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var columns = line.TrimEnd('\r').Split('\t');
            if (columns.Length < 12 ||
                !decimal.TryParse(columns[10], NumberStyles.Number, CultureInfo.InvariantCulture, out var confidence) ||
                confidence < 0 ||
                string.IsNullOrWhiteSpace(columns[11]))
            {
                continue;
            }

            if (text.Length > 0)
            {
                text.Append(' ');
            }
            text.Append(columns[11]);
            totalConfidence += confidence;
            recognizedWords++;
        }

        return new OcrDocumentText(
            text.ToString(),
            recognizedWords == 0 ? null : totalConfidence / recognizedWords);
    }

    private static int ParsePageNumber(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return int.TryParse(name.AsSpan(name.LastIndexOf('-') + 1), out var page) ? page : int.MaxValue;
    }

    private static string TrimForLog(string value) =>
        string.IsNullOrWhiteSpace(value) ? "No diagnostic details were returned." :
        value.Length <= 300 ? value.Trim() : value[..300].Trim();

    private static void KillProcess(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }
    }
}
