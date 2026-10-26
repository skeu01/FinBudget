using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Finance;

public sealed class ExpenseItemPersistenceTests
{
    [Fact]
    public void ItemsConversion_ReadsLegacyConceptAndWritesStructuredRecords()
    {
        var options = new DbContextOptionsBuilder<FinancialDbContext>()
            .UseNpgsql("Host=localhost;Database=finbudget")
            .Options;
        using var context = new FinancialDbContext(options);
        var converter = context.Model.FindEntityType(typeof(Expense))!
            .FindProperty(nameof(Expense.Items))!
            .GetValueConverter()!;

        var legacyItems = (List<ExpenseItem>)converter.ConvertFromProvider(
            """[{"Concept":"Milk","Price":2.5}]""")!;
        legacyItems.Should().ContainSingle().Which.Should().Be(new ExpenseItem("Milk", 1, 2.5m));

        var json = (string)converter.ConvertToProvider(
            new List<ExpenseItem> { new("Milk", 2, 2.5m) })!;
        json.Should().Be("""[{"Description":"Milk","Quantity":2,"Price":2.5}]""");
    }
}
