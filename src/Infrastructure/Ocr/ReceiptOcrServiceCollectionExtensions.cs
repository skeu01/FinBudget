using Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Ocr;

public static class ReceiptOcrServiceCollectionExtensions
{
    public static IServiceCollection AddReceiptOcr(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ReceiptOcrOptions>()
            .Bind(configuration.GetSection(ReceiptOcrOptions.SectionName))
            .Validate(options => options.Provider is "Tesseract" or "AzureDocumentIntelligence",
                "ReceiptOcr:Provider must be Tesseract or AzureDocumentIntelligence.")
            .Validate(options => options.MaxFileSizeBytes is > 0 and <= 25 * 1024 * 1024,
                "ReceiptOcr:MaxFileSizeBytes must be between 1 byte and 25 MiB.")
            .Validate(options => options.MaxPdfPages is > 0 and <= 50,
                "ReceiptOcr:MaxPdfPages must be between 1 and 50.")
            .Validate(options => options.ProcessTimeoutSeconds is > 0 and <= 300,
                "ReceiptOcr:ProcessTimeoutSeconds must be between 1 and 300.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.StoragePath),
                "ReceiptOcr:StoragePath is required.")
            .Validate(options => options.Provider != "AzureDocumentIntelligence" ||
                                 Uri.TryCreate(options.AzureEndpoint, UriKind.Absolute, out var endpoint) &&
                                 endpoint.Scheme == Uri.UriSchemeHttps &&
                                 !string.IsNullOrWhiteSpace(options.AzureApiKey),
                "AzureEndpoint and AzureApiKey are required for AzureDocumentIntelligence.")
            .ValidateOnStart();

        services.AddSingleton<IReceiptFileStore, LocalReceiptFileStore>();
        services.AddScoped<IReceiptOcrProvider>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ReceiptOcrOptions>>().Value;
            return options.Provider switch
            {
                "Tesseract" => ActivatorUtilities.CreateInstance<TesseractOcrProvider>(provider),
                "AzureDocumentIntelligence" => ActivatorUtilities.CreateInstance<AzureDocumentIntelligenceOcrProvider>(
                    provider, provider.GetRequiredService<IHttpClientFactory>().CreateClient("ReceiptOcrAzure")),
                _ => throw new InvalidOperationException("The configured receipt OCR provider is not supported.")
            };
        });
        services.AddScoped<IReceiptOcrService, ReceiptOcrService>();
        return services;
    }
}
