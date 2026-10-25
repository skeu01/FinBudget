namespace Domain.ValueObjects;

public sealed record MonthPeriod
{
    public MonthPeriod(int year, int month)
    {
        if (year is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }

        Year = year;
        Month = month;
    }

    public int Year { get; }

    public int Month { get; }

    public DateOnly StartDate => new(Year, Month, 1);

    public DateOnly EndDate => StartDate.AddMonths(1).AddDays(-1);

    public bool Contains(DateOnly date) => date >= StartDate && date <= EndDate;

    public override string ToString() => $"{Year:D4}-{Month:D2}";
}
