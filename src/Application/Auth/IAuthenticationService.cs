namespace Application.Auth;

public interface IAuthenticationService
{
    Task<AuthOperationResult> RegisterAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken);

    Task<AuthOperationResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken);

    Task<AuthOperationResult> RefreshAsync(
        string? refreshToken,
        CancellationToken cancellationToken);

    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken);

    Task<AuthUserResponse?> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
}
