namespace Domain.ValueObjects;

public sealed record Percentage
{
    public Percentage(decimal value)
    {
        if (value is <= 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Percentage must be greater than zero and at most 100.");
        }

        Value = value;
    }

    public decimal Value { get; }

    public bool IsReachedBy(decimal current, decimal target) =>
        target > 0 && current / target * 100 >= Value;
}
