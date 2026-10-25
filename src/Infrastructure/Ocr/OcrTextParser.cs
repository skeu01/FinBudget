using System.Globalization;
using System.Text.RegularExpressions;
using Domain.ValueObjects;

namespace Infrastructure.Ocr;

internal static partial class OcrTextParser
{
    private static readonly string[] SupportedDateFormats =
    [
        "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
        "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd", "yyyy/MM/dd"
    ];

    [GeneratedRegex(@"(?<!\d)(\d{1,2}[./-]\d{1,2}[./-]\d{4}|\d{4}[/-]\d{1,2}[/-]\d{1,2})(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex DatePattern();

    [GeneratedRegex(@"(?<!\d)(\d{1,3}(?:[.,\s]\d{3})*(?:[.,]\d{1,2})?|\d+(?:[.,]\d{1,2})?)(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex AmountPattern();

    [GeneratedRegex(@"\b(?:total(?:\s+a\s+pagar)?|importe\s+(?:total|a\s+pagar)|amount\s+due)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TotalLabelPattern();

    [GeneratedRegex(@"\b(?:iva|vat|impuesto(?:s)?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VatLabelPattern();

    public static OcrExtractedData Parse(string text, CurrencyCode currency, decimal? confidence)
    {
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToArray();

        var occurredOn = FindDate(string.Join(Environment.NewLine, lines));
        var merchant = FindMerchant(lines);
        var total = FindAmount(lines, TotalLabelPattern(), currency, skipVatLines: true);
        var vat = FindAmount(lines, VatLabelPattern(), currency, skipVatLines: false);
        var confidenceValue = confidence is > 0 and <= 100 ? new Percentage(confidence.Value) : null;

        return new OcrExtractedData(merchant, occurredOn, total, confidenceValue, vat);
    }

    private static DateOnly? FindDate(string text)
    {
        foreach (Match match in DatePattern().Matches(text))
        {
            var value = match.Groups[1].Value.Replace('.', '/').Replace('-', '/');
            var formats = value.Length > 0 && value[0] is >= '0' and <= '9' && value.Length == 10 &&
                          value[4] == '/'
                ? new[] { "yyyy/MM/dd" }
                : SupportedDateFormats;
            if (DateOnly.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
                date.Year >= 2000 && date <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            {
                return date;
            }
        }

        return null;
    }

    private static string? FindMerchant(IEnumerable<string> lines)
    {
        string[] excludedTerms = ["ticket", "factura", "recibo", "cif", "nif", "iva", "vat", "total", "fecha", "gracias"];
        return lines.FirstOrDefault(line =>
            line.Length is >= 3 and <= 100 &&
            !excludedTerms.Any(term => line.Contains(term, StringComparison.OrdinalIgnoreCase)) &&
            line.Any(char.IsLetter));
    }

    private static Money? FindAmount(
        IEnumerable<string> lines,
        Regex labelPattern,
        CurrencyCode currency,
        bool skipVatLines)
    {
        var candidates = lines
            .Where(line => labelPattern.IsMatch(line))
            .Where(line => !skipVatLines || !VatLabelPattern().IsMatch(line))
            .Where(line => !line.Contains("subtotal", StringComparison.OrdinalIgnoreCase))
            .Select(line => FindLastAmount(line))
            .Where(amount => amount.HasValue)
            .ToArray();

        return candidates.Length == 0 ? null : new Money(candidates[^1]!.Value, currency);
    }

    private static decimal? FindLastAmount(string line)
    {
        var matches = AmountPattern().Matches(line);
        if (matches.Count == 0)
        {
            return null;
        }

        var lastMatch = matches[^1];
        if (line.AsSpan(lastMatch.Index + lastMatch.Length).TrimStart().StartsWith("%", StringComparison.Ordinal))
        {
            return null;
        }

        return TryParseAmount(lastMatch.Groups[1].Value);
    }

    private static decimal? TryParseAmount(string value)
    {
        var normalized = value.Replace(" ", string.Empty);
        var lastComma = normalized.LastIndexOf(',');
        var lastDot = normalized.LastIndexOf('.');
        var decimalSeparator = Math.Max(lastComma, lastDot);

        if (decimalSeparator >= 0)
        {
            var decimalDigits = normalized.Length - decimalSeparator - 1;
            if (decimalDigits is 1 or 2)
            {
                var integerPart = normalized[..decimalSeparator].Replace(",", string.Empty).Replace(".", string.Empty);
                normalized = $"{integerPart}.{normalized[(decimalSeparator + 1)..]}";
            }
            else
            {
                normalized = normalized.Replace(",", string.Empty).Replace(".", string.Empty);
            }
        }

        return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : null;
    }
}
