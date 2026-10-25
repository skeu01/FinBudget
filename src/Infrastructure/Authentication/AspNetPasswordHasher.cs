using Application.Auth;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Authentication;

public sealed class AspNetPasswordHasher(IPasswordHasher<User> passwordHasher) : IPasswordHasher
{
    public string Hash(User user, string password) =>
        passwordHasher.HashPassword(user, password);

    public PasswordVerification Verify(User user, string password) =>
        passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordVerification.Succeeded,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.RehashRequired,
            _ => PasswordVerification.Failed
        };
}
