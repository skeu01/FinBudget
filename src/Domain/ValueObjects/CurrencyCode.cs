namespace Domain.ValueObjects;

public sealed record CurrencyCode
{
    public CurrencyCode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException("Currency code must contain three ASCII letters.", nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
