using Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Presentation.Contracts.Requests;
using Presentation.Contracts.Responses;
using AuthSessionApiResponse = Presentation.Contracts.Responses.AuthSessionResponse;

namespace Presentation.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("authentication")]
public sealed class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
    private const string RefreshCookieName = "__Host-family-finance-refresh";
    private static readonly TimeSpan RefreshCookieLifetime = TimeSpan.FromDays(30);

    /// <summary>Registra una cuenta.</summary>
    [AllowAnonymous]
    [HttpPost("register", Name = "Register")]
    [ProducesResponseType(typeof(AuthenticatedUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthenticatedUserResponse>> Register(
        RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.RegisterAsync(
            new RegisterUserCommand(request.Email, request.DisplayName, request.Password), cancellationToken);
        if (result.Failure == AuthFailure.EmailAlreadyRegistered) return Conflict();
        if (!result.Succeeded || result.User is null) return ValidationProblem();
        var user = new AuthenticatedUserResponse(result.User.Id, result.User.Email, result.User.DisplayName, result.User.Role);
        return CreatedAtAction(nameof(GetProfile), routeValues: null, value: user);
    }

    /// <summary>Inicia sesión y emite tokens de acceso.</summary>
    [AllowAnonymous]
    [HttpPost("login", Name = "Login")]
    [ProducesResponseType(typeof(AuthSessionApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthSessionApiResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginAsync(
            new LoginCommand(request.Email, request.Password), cancellationToken);
        if (!result.Succeeded || result.Session is null) return Unauthorized();
        SetRefreshCookie(result.Session.RefreshToken);
        return Ok(ToResponse(result.Session));
    }

    /// <summary>Rota el refresh token y emite un nuevo access token.</summary>
    [AllowAnonymous]
    [HttpPost("refresh", Name = "RefreshToken")]
    [ProducesResponseType(typeof(AuthSessionApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthSessionApiResponse>> Refresh(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken);
        var result = await authenticationService.RefreshAsync(refreshToken, cancellationToken);
        if (!result.Succeeded || result.Session is null)
        {
            DeleteRefreshCookie();
            return Unauthorized();
        }
        SetRefreshCookie(result.Session.RefreshToken);
        return Ok(ToResponse(result.Session));
    }

    /// <summary>Revoca el refresh token de la sesión actual.</summary>
    [AllowAnonymous]
    [HttpPost("logout", Name = "Logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken);
        await authenticationService.LogoutAsync(refreshToken, cancellationToken);
        DeleteRefreshCookie();
        return NoContent();
    }

    /// <summary>Devuelve el perfil del usuario autenticado.</summary>
    [Authorize]
    [HttpGet("me", Name = "GetCurrentUser")]
    [ProducesResponseType(typeof(AuthenticatedUserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticatedUserResponse>> GetProfile(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId)) return Unauthorized();
        var user = await authenticationService.GetProfileAsync(userId, cancellationToken);
        if (user is null) return Unauthorized();
        return Ok(new AuthenticatedUserResponse(user.Id, user.Email, user.DisplayName, user.Role));
    }

    private static AuthSessionApiResponse ToResponse(Application.Auth.AuthSessionResponse session) =>
        new(session.AccessToken, session.AccessTokenExpiresAt,
            new AuthenticatedUserResponse(session.User.Id, session.User.Email, session.User.DisplayName, session.User.Role));

    private void SetRefreshCookie(string token) =>
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
            MaxAge = RefreshCookieLifetime
        });

    private void DeleteRefreshCookie() =>
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
}
