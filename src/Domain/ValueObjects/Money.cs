namespace Domain.ValueObjects;

public sealed record Money
{
    private Money()
    {
        Currency = null!;
    }

    public Money(decimal amount, CurrencyCode currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Money amount cannot be negative.");
        }

        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; private set; }

    public CurrencyCode Currency { get; private set; }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        if (other.Amount > Amount)
        {
            throw new InvalidOperationException("The result cannot be negative.");
        }

        return new Money(Amount - other.Amount, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException("Money values must use the same currency.");
        }
    }
}
