using System.Net.Mail;

namespace Domain.ValueObjects;

public sealed record EmailAddress
{
    public EmailAddress(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim();
        try
        {
            var parsed = new MailAddress(normalized);
            if (!string.Equals(parsed.Address, normalized, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException();
            }
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("Email address is invalid.", nameof(value), exception);
        }

        Value = normalized.ToLowerInvariant();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
