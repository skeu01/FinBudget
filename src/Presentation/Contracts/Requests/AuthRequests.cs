using System.ComponentModel.DataAnnotations;

namespace Presentation.Contracts.Requests;

/// <summary>Datos necesarios para registrar una cuenta.</summary>
public sealed record RegisterRequest
{
    [Required, EmailAddress, MaxLength(320)]
    public required string Email { get; init; }

    [Required, StringLength(120, MinimumLength = 1)]
    public required string DisplayName { get; init; }

    [Required, StringLength(128, MinimumLength = 12)]
    public required string Password { get; init; }
}

/// <summary>Credenciales para iniciar sesión.</summary>
public sealed record LoginRequest
{
    [Required, EmailAddress, MaxLength(320)]
    public required string Email { get; init; }

    [Required, StringLength(128, MinimumLength = 1)]
    public required string Password { get; init; }
}
