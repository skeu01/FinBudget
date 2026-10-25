namespace Application.Auth;

public sealed record RegisterUserCommand(string Email, string DisplayName, string Password);

public sealed record LoginCommand(string Email, string Password);

public sealed record AuthUserResponse(Guid Id, string Email, string DisplayName, string Role);

public sealed record AuthSessionResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    AuthUserResponse User);

public enum AuthFailure
{
    None,
    InvalidInput,
    EmailAlreadyRegistered,
    InvalidCredentials,
    InvalidRefreshToken
}

public sealed record AuthOperationResult(
    AuthFailure Failure,
    AuthUserResponse? User = null,
    AuthSessionResponse? Session = null)
{
    public bool Succeeded => Failure == AuthFailure.None;

    public static AuthOperationResult Success(AuthUserResponse user) =>
        new(AuthFailure.None, User: user);

    public static AuthOperationResult Success(AuthSessionResponse session) =>
        new(AuthFailure.None, User: session.User, Session: session);

    public static AuthOperationResult Fail(AuthFailure failure) => new(failure);
}
