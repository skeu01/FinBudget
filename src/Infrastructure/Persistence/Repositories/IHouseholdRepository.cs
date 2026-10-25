using Domain.Common;

namespace Infrastructure.Persistence.Repositories;

public interface IHouseholdRepository<TEntity> : IRepository<TEntity>
    where TEntity : Entity, IHouseholdScoped
{
    Task<TEntity?> GetForHouseholdAsync(
        Guid householdId,
        Guid entityId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> ListForHouseholdAsync(
        Guid householdId,
        CancellationToken cancellationToken = default);
}
