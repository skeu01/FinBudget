using Application.Auth;
using Domain.Entities;
using Domain.ValueObjects;
using Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Persistence.Repositories;

public sealed class EfAuthenticationRepository(FinancialDbContext dbContext) : IAuthenticationRepository
{
    public Task<User?> FindUserByEmailAsync(
        EmailAddress email,
        CancellationToken cancellationToken) =>
        dbContext.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task<User?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public async Task<bool> AddUserAsync(User user, CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_users_Email"
            })
        {
            dbContext.Entry(user).State = EntityState.Detached;
            return false;
        }
    }

    public Task<RefreshToken?> FindRefreshTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken) =>
        dbContext.RefreshTokens.SingleOrDefaultAsync(
            token => token.TokenHash == tokenHash,
            cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> FindActiveRefreshTokensAsync(
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null && token.ExpiresAt > now)
            .ToListAsync(cancellationToken);

    public Task AddRefreshTokenAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken) =>
        dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken).AsTask();

    public async Task<bool> SaveRefreshTokenRotationAsync(
        RefreshToken currentToken,
        RefreshToken replacementToken,
        CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens.AddAsync(replacementToken, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.Entry(currentToken).State = EntityState.Detached;
            dbContext.Entry(replacementToken).State = EntityState.Detached;
            return false;
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
