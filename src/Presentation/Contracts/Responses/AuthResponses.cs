namespace Presentation.Contracts.Responses;

/// <summary>Información pública de una cuenta autenticada.</summary>
public sealed record AuthenticatedUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string Role);

/// <summary>Sesión autenticada; el refresh token se entrega únicamente en cookie HttpOnly.</summary>
public sealed record AuthSessionResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    AuthenticatedUserResponse User);
