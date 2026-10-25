using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Infrastructure.Ocr;

public sealed class AzureDocumentIntelligenceOcrProvider(
    HttpClient httpClient,
    IOptions<ReceiptOcrOptions> options) : IReceiptOcrProvider
{
    private readonly ReceiptOcrOptions _options = options.Value;

    public string Name => "AzureDocumentIntelligence";

    public async Task<OcrDocumentText> RecognizeAsync(
        string filePath,
        string mediaType,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(_options.AzureEndpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(_options.AzureApiKey))
        {
            throw new OcrProviderException("provider_not_configured", "Azure OCR credentials are not configured.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.ProcessTimeoutSeconds));
        try
        {
            return await AnalyzeAsync(filePath, mediaType, endpoint, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OcrProviderException("processing_timeout", "OCR processing exceeded the configured time limit.");
        }
        catch (HttpRequestException exception)
        {
            throw new OcrProviderException("provider_unavailable", "Azure OCR could not be reached.", exception);
        }
        catch (JsonException exception)
        {
            throw new OcrProviderException("invalid_provider_response", "Azure OCR returned an invalid response.", exception);
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or KeyNotFoundException or UriFormatException)
        {
            throw new OcrProviderException("invalid_provider_response", "Azure OCR returned an invalid response.", exception);
        }
    }

    private async Task<OcrDocumentText> AnalyzeAsync(
        string filePath,
        string mediaType,
        Uri endpoint,
        CancellationToken cancellationToken)
    {
        var requestUri = new Uri(endpoint, "documentintelligence/documentModels/prebuilt-read:analyze?api-version=2024-11-30");
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Add("Ocp-Apim-Subscription-Key", _options.AzureApiKey);
        request.Content = new ByteArrayContent(await File.ReadAllBytesAsync(filePath, cancellationToken));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            throw new OcrProviderException("provider_failed", "Azure OCR rejected the document.");
        }

        var operationLocation = response.Headers.Location;
        if (operationLocation is null && response.Headers.TryGetValues("Operation-Location", out var values))
        {
            operationLocation = new Uri(values.Single(), UriKind.RelativeOrAbsolute);
        }

        var operationUri = operationLocation is null
            ? null
            : operationLocation.IsAbsoluteUri ? operationLocation : new Uri(endpoint, operationLocation);
        if (operationUri is null ||
            operationUri.Scheme != endpoint.Scheme ||
            !string.Equals(operationUri.Authority, endpoint.Authority, StringComparison.OrdinalIgnoreCase))
        {
            throw new OcrProviderException("invalid_provider_response", "Azure OCR returned an invalid operation URL.");
        }

        return await PollForResultAsync(operationUri, cancellationToken);
    }

    private async Task<OcrDocumentText> PollForResultAsync(
        Uri operationUri,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, operationUri);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _options.AzureApiKey);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new OcrProviderException("provider_failed", "Azure OCR could not complete the document analysis.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            var status = root.TryGetProperty("status", out var statusElement)
                ? statusElement.GetString()
                : null;
            if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase))
            {
                var content = root.GetProperty("analyzeResult").GetProperty("content").GetString() ?? string.Empty;
                return new OcrDocumentText(content, null);
            }

            if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                throw new OcrProviderException("processing_failed", "Azure OCR could not read the document.");
            }

            if (!string.Equals(status, "running", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(status, "notStarted", StringComparison.OrdinalIgnoreCase))
            {
                throw new OcrProviderException("invalid_provider_response", "Azure OCR returned an unknown operation status.");
            }

        }
    }
}
