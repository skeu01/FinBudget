using Domain.Entities;

namespace Application.Auth;

public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(User user, DateTimeOffset now);
}

public sealed record IssuedAccessToken(string Value, DateTimeOffset ExpiresAt);
