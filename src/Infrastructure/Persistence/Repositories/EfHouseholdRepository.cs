using Domain.Common;
using Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class EfHouseholdRepository<TEntity>(FinancialDbContext dbContext)
    : EfRepository<TEntity>(dbContext), IHouseholdRepository<TEntity>
    where TEntity : Entity, IHouseholdScoped
{
    public Task<TEntity?> GetForHouseholdAsync(
        Guid householdId,
        Guid entityId,
        CancellationToken cancellationToken = default) =>
        Entities.FirstOrDefaultAsync(
            entity => entity.HouseholdId == householdId && entity.Id == entityId,
            cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListForHouseholdAsync(
        Guid householdId,
        CancellationToken cancellationToken = default) =>
        await Entities
            .Where(entity => entity.HouseholdId == householdId)
            .ToListAsync(cancellationToken);
}
