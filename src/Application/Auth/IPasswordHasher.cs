using Domain.Entities;

namespace Application.Auth;

public interface IPasswordHasher
{
    string Hash(User user, string password);

    PasswordVerification Verify(User user, string password);
}

public enum PasswordVerification
{
    Failed,
    Succeeded,
    RehashRequired
}
