using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Finance;

public sealed class ExpenseAndBudgetTests
{
    private static readonly CurrencyCode Eur = new("EUR");

    [Fact]
    public void Expense_RequiresPositiveAmount()
    {
        var act = () => new Expense(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new Money(0, Eur), new DateOnly(2026, 10, 25));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Expense_NormalizesOptionalTextAndCannotBeChangedAfterDeletion()
    {
        var expense = new Expense(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new Money(32.50m, Eur), new DateOnly(2026, 10, 25),
            description: "  groceries  ", merchant: "  Market  ");

        expense.Description.Should().Be("groceries");
        expense.Merchant.Should().Be("Market");
        expense.Delete(DateTimeOffset.UtcNow);

        var act = () => expense.Update(
            Guid.NewGuid(), new Money(40, Eur), new DateOnly(2026, 10, 26), null, null, null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Budget_ReportsThresholdAndOverspendSeparately()
    {
        var budget = new Budget(
            Guid.NewGuid(), Guid.NewGuid(), new MonthPeriod(2026, 10),
            new Money(100, Eur), new Percentage(80), Guid.NewGuid());

        budget.IsAlertThresholdReached(new Money(80, Eur)).Should().BeTrue();
        budget.IsOverspent(new Money(80, Eur)).Should().BeFalse();
        budget.IsOverspent(new Money(100.01m, Eur)).Should().BeTrue();
    }

    [Fact]
    public void Budget_RejectsSpentAmountInDifferentCurrency()
    {
        var budget = new Budget(
            Guid.NewGuid(), Guid.NewGuid(), new MonthPeriod(2026, 10),
            new Money(100, Eur), new Percentage(80), Guid.NewGuid());

        var act = () => budget.IsOverspent(new Money(50, new CurrencyCode("USD")));

        act.Should().Throw<InvalidOperationException>();
    }
}
