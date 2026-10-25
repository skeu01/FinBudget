using Application.Auth;
using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Authentication;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task RegisterAsync_RejectsWeakPasswordWithoutCreatingUser()
    {
        var repository = new InMemoryAuthenticationRepository();
        var service = CreateService(repository);

        var result = await service.RegisterAsync(
            new RegisterUserCommand("family@example.com", "Family", "weak"),
            CancellationToken.None);

        result.Failure.Should().Be(AuthFailure.InvalidInput);
        repository.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task RegisterAsync_RejectsDuplicateEmail()
    {
        var repository = new InMemoryAuthenticationRepository();
        var service = CreateService(repository);
        var command = new RegisterUserCommand("family@example.com", "Family", "StrongPassword123");
        (await service.RegisterAsync(command, CancellationToken.None)).Succeeded.Should().BeTrue();

        var result = await service.RegisterAsync(command with { DisplayName = "Another name" }, CancellationToken.None);

        result.Failure.Should().Be(AuthFailure.EmailAlreadyRegistered);
        repository.Users.Should().ContainSingle();
    }

    [Fact]
    public async Task LoginAsync_issuesSessionAndLogoutRevokesRefreshToken()
    {
        var repository = new InMemoryAuthenticationRepository();
        var service = CreateService(repository);
        var registered = await service.RegisterAsync(
            new RegisterUserCommand("family@example.com", "Family", "StrongPassword123"),
            CancellationToken.None);

        var login = await service.LoginAsync(
            new LoginCommand("family@example.com", "StrongPassword123"),
            CancellationToken.None);

        login.Succeeded.Should().BeTrue();
        login.Session.Should().NotBeNull();
        login.Session!.AccessToken.Should().Be("access-token");
        login.Session.RefreshToken.Should().NotBeNullOrWhiteSpace();
        repository.RefreshTokens.Should().ContainSingle();

        await service.LogoutAsync(login.Session.RefreshToken, CancellationToken.None);

        repository.RefreshTokens.Single().IsActive(DateTimeOffset.UtcNow).Should().BeFalse();
        registered.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_RejectsIncorrectPassword()
    {
        var repository = new InMemoryAuthenticationRepository();
        var service = CreateService(repository);
        await service.RegisterAsync(
            new RegisterUserCommand("family@example.com", "Family", "StrongPassword123"),
            CancellationToken.None);

        var result = await service.LoginAsync(
            new LoginCommand("family@example.com", "WrongPassword123"),
            CancellationToken.None);

        result.Failure.Should().Be(AuthFailure.InvalidCredentials);
        repository.RefreshTokens.Should().BeEmpty();
    }

    [Fact]
    public async Task RefreshAsync_RotatesTokenAndKeepsItsFamily()
    {
        var repository = new InMemoryAuthenticationRepository();
        var service = CreateService(repository);
        await service.RegisterAsync(
            new RegisterUserCommand("family@example.com", "Family", "StrongPassword123"),
            CancellationToken.None);
        var login = await service.LoginAsync(
            new LoginCommand("family@example.com", "StrongPassword123"),
            CancellationToken.None);
        var original = repository.RefreshTokens.Single();

        var refreshed = await service.RefreshAsync(login.Session!.RefreshToken, CancellationToken.None);

        refreshed.Succeeded.Should().BeTrue();
        refreshed.Session.Should().NotBeNull();
        refreshed.Session!.RefreshToken.Should().NotBe(login.Session.RefreshToken);
        repository.RefreshTokens.Should().HaveCount(2);
        original.IsActive(DateTimeOffset.UtcNow).Should().BeFalse();
        repository.RefreshTokens[1].FamilyId.Should().Be(original.FamilyId);
        repository.RefreshTokens[1].IsActive(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    private static AuthenticationService CreateService(InMemoryAuthenticationRepository repository) =>
        new(repository, new TestPasswordHasher(), new TestAccessTokenIssuer());

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(User user, string password) => $"hashed:{password}";

        public PasswordVerification Verify(User user, string password) =>
            user.PasswordHash == Hash(user, password)
                ? PasswordVerification.Succeeded
                : PasswordVerification.Failed;
    }

    private sealed class TestAccessTokenIssuer : IAccessTokenIssuer
    {
        public IssuedAccessToken Issue(User user, DateTimeOffset now) =>
            new("access-token", now.AddMinutes(10));
    }

    private sealed class InMemoryAuthenticationRepository : IAuthenticationRepository
    {
        public List<User> Users { get; } = [];
        public List<RefreshToken> RefreshTokens { get; } = [];

        public Task<User?> FindUserByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
            Task.FromResult(Users.SingleOrDefault(user => user.Email == email));

        public Task<User?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Users.SingleOrDefault(user => user.Id == userId));

        public Task<bool> AddUserAsync(User user, CancellationToken cancellationToken)
        {
            if (Users.Any(existing => existing.Email == user.Email)) return Task.FromResult(false);
            Users.Add(user);
            return Task.FromResult(true);
        }

        public Task<RefreshToken?> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) =>
            Task.FromResult(RefreshTokens.SingleOrDefault(token => token.TokenHash == tokenHash));

        public Task<IReadOnlyList<RefreshToken>> FindActiveRefreshTokensAsync(
            Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RefreshToken>>(
                RefreshTokens.Where(token => token.UserId == userId && token.IsActive(now)).ToList());

        public Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
        {
            RefreshTokens.Add(refreshToken);
            return Task.CompletedTask;
        }

        public Task<bool> SaveRefreshTokenRotationAsync(
            RefreshToken currentToken, RefreshToken replacementToken, CancellationToken cancellationToken)
        {
            if (!RefreshTokens.Contains(currentToken) ||
                currentToken.ReplacedByTokenHash != replacementToken.TokenHash)
                return Task.FromResult(false);

            RefreshTokens.Add(replacementToken);
            return Task.FromResult(true);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
