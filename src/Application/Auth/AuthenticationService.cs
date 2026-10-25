using System.Security.Cryptography;
using System.Text;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Auth;

public sealed class AuthenticationService(
    IAuthenticationRepository repository,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer accessTokenIssuer) : IAuthenticationService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);
    private static readonly TimeSpan AccessTokenLifetimeLimit = TimeSpan.FromMinutes(30);

    public async Task<AuthOperationResult> RegisterAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email) ||
            string.IsNullOrWhiteSpace(command.DisplayName) ||
            command.Email.Length > 320 ||
            command.DisplayName.Trim().Length > 120 ||
            !IsValidPassword(command.Password))
        {
            return AuthOperationResult.Fail(AuthFailure.InvalidInput);
        }

        EmailAddress email;
        try
        {
            email = new EmailAddress(command.Email);
        }
        catch (ArgumentException)
        {
            return AuthOperationResult.Fail(AuthFailure.InvalidInput);
        }

        if (await repository.FindUserByEmailAsync(email, cancellationToken) is not null)
        {
            return AuthOperationResult.Fail(AuthFailure.EmailAlreadyRegistered);
        }

        var user = new User(email, command.DisplayName, UserStatus.Active);
        user.SetPasswordHash(passwordHasher.Hash(user, command.Password));
        if (!await repository.AddUserAsync(user, cancellationToken))
        {
            return AuthOperationResult.Fail(AuthFailure.EmailAlreadyRegistered);
        }

        return AuthOperationResult.Success(ToResponse(user));
    }

    public async Task<AuthOperationResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrEmpty(command.Password))
        {
            return AuthOperationResult.Fail(AuthFailure.InvalidCredentials);
        }

        EmailAddress email;
        try
        {
            email = new EmailAddress(command.Email);
        }
        catch (ArgumentException)
        {
            return AuthOperationResult.Fail(AuthFailure.InvalidCredentials);
        }

        var user = await repository.FindUserByEmailAsync(email, cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
        {
            return AuthOperationResult.Fail(AuthFailure.InvalidCredentials);
        }

        var verification = passwordHasher.Verify(user, command.Password);
        if (verification == PasswordVerification.Failed)
        {
            return AuthOperationResult.Fail(AuthFailure.InvalidCredentials);
        }

        if (verification == PasswordVerification.RehashRequired)
        {
            user.SetPasswordHash(passwordHasher.Hash(user, command.Password));
        }

        var now = DateTimeOffset.UtcNow;
        var rawRefreshToken = CreateRefreshToken();
        var refreshToken = new RefreshToken(
            user.Id,
            Guid.NewGuid(),
            HashRefreshToken(rawRefreshToken),
            now,
            now.Add(RefreshTokenLifetime));
        await repository.AddRefreshTokenAsync(refreshToken, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return AuthOperationResult.Success(CreateSession(user, rawRefreshToken, now));
    }

    public async Task<AuthOperationResult> RefreshAsync(
        string? rawRefreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return AuthOperationResult.Fail(AuthFailure.InvalidRefreshToken);
        }

        var now = DateTimeOffset.UtcNow;
        var tokenHash = HashRefreshToken(rawRefreshToken);
        var current = await repository.FindRefreshTokenAsync(tokenHash, cancellationToken);
        if (current is null)
        {
            return AuthOperationResult.Fail(AuthFailure.InvalidRefreshToken);
        }

        if (!current.IsActive(now))
        {
            if (current.RevokedAt.HasValue)
            {
                foreach (var activeToken in await repository.FindActiveRefreshTokensAsync(
                             current.UserId,
                             now,
                             cancellationToken))
                {
                    activeToken.Revoke(now);
                }

                await repository.SaveChangesAsync(cancellationToken);
            }

            return AuthOperationResult.Fail(AuthFailure.InvalidRefreshToken);
        }

        var user = await repository.FindUserByIdAsync(current.UserId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
        {
            current.Revoke(now);
            await repository.SaveChangesAsync(cancellationToken);
            return AuthOperationResult.Fail(AuthFailure.InvalidRefreshToken);
        }

        var replacementRawToken = CreateRefreshToken();
        var replacementHash = HashRefreshToken(replacementRawToken);
        current.Revoke(now, replacementHash);
        var replacementToken = new RefreshToken(
            user.Id,
            current.FamilyId,
            replacementHash,
            now,
            now.Add(RefreshTokenLifetime));
        if (!await repository.SaveRefreshTokenRotationAsync(current, replacementToken, cancellationToken))
        {
            return AuthOperationResult.Fail(AuthFailure.InvalidRefreshToken);
        }

        return AuthOperationResult.Success(CreateSession(user, replacementRawToken, now));
    }

    public async Task LogoutAsync(string? rawRefreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return;
        }

        var token = await repository.FindRefreshTokenAsync(
            HashRefreshToken(rawRefreshToken),
            cancellationToken);
        if (token is null || token.RevokedAt.HasValue)
        {
            return;
        }

        token.Revoke(DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthUserResponse?> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await repository.FindUserByIdAsync(userId, cancellationToken);
        return user is { Status: UserStatus.Active } ? ToResponse(user) : null;
    }

    private AuthSessionResponse CreateSession(User user, string rawRefreshToken, DateTimeOffset now)
    {
        var accessToken = accessTokenIssuer.Issue(user, now);
        if (accessToken.ExpiresAt <= now || accessToken.ExpiresAt > now.Add(AccessTokenLifetimeLimit))
        {
            throw new InvalidOperationException("Access token issuer returned an invalid expiration.");
        }

        return new AuthSessionResponse(
            accessToken.Value,
            accessToken.ExpiresAt,
            rawRefreshToken,
            ToResponse(user));
    }

    private static AuthUserResponse ToResponse(User user) =>
        new(user.Id, user.Email.Value, user.DisplayName, user.Role.ToString());

    private static bool IsValidPassword(string password) =>
        !string.IsNullOrEmpty(password) &&
        password.Length is >= 12 and <= 128 &&
        password.Any(char.IsUpper) &&
        password.Any(char.IsLower) &&
        password.Any(char.IsDigit);

    private static string CreateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
