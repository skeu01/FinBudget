using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence.Contexts;

public sealed class FinancialDbContextFactory : IDesignTimeDbContextFactory<FinancialDbContext>
{
    public FinancialDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("FINANCIAL_DB_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set FINANCIAL_DB_CONNECTION before running Entity Framework design-time commands.");
        }

        var options = new DbContextOptionsBuilder<FinancialDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new FinancialDbContext(options);
    }
}
