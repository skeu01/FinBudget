using Domain.Common;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class User : Entity, IAggregateRoot
{
    public User(
        EmailAddress email,
        string displayName,
        UserStatus status = UserStatus.PendingVerification,
        string passwordHash = "")
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Email = email;
        DisplayName = displayName.Trim();
        Status = status;
        PasswordHash = passwordHash;
        Role = SystemRole.User;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public EmailAddress Email { get; private set; }

    public string DisplayName { get; private set; }

    public UserStatus Status { get; private set; }

    public SystemRole Role { get; private set; }

    public string PasswordHash { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    public void ChangeDisplayName(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName.Trim();
    }

    public void VerifyEmail(DateTimeOffset verifiedAt)
    {
        if (Status == UserStatus.Deactivated)
        {
            throw new InvalidOperationException("A deactivated user cannot verify their email.");
        }

        EmailVerifiedAt = verifiedAt;
        if (Status == UserStatus.PendingVerification)
        {
            Status = UserStatus.Active;
        }
    }

    public void ChangeStatus(UserStatus status)
    {
        Status = status;
    }

    public void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }

    public void ChangeRole(SystemRole role)
    {
        Role = role;
    }
}
