using Domain.Entities;
using Infrastructure.Persistence.Contexts;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<FinancialDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IHouseholdRepository<>), typeof(EfHouseholdRepository<>));
        services.AddScoped<IRepository<User>, EfRepository<User>>();
        services.AddScoped<IRepository<Household>, EfRepository<Household>>();
        services.AddScoped<IRepository<HouseholdMember>, EfRepository<HouseholdMember>>();
        services.AddScoped<IHouseholdRepository<Category>, EfHouseholdRepository<Category>>();
        services.AddScoped<IHouseholdRepository<Expense>, EfHouseholdRepository<Expense>>();
        services.AddScoped<IHouseholdRepository<Income>, EfHouseholdRepository<Income>>();
        services.AddScoped<IHouseholdRepository<Budget>, EfHouseholdRepository<Budget>>();
        services.AddScoped<IHouseholdRepository<SavingsGoal>, EfHouseholdRepository<SavingsGoal>>();
        services.AddScoped<IHouseholdRepository<SavingsGoalContribution>, EfHouseholdRepository<SavingsGoalContribution>>();
        services.AddScoped<IHouseholdRepository<Receipt>, EfHouseholdRepository<Receipt>>();
        services.AddScoped<IRepository<OcrResult>, EfRepository<OcrResult>>();
        return services;
    }
}
