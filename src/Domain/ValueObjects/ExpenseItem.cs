namespace Domain.ValueObjects;

public sealed record ExpenseItem(string Description, decimal Quantity, decimal Price);
