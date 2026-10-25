using Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Contracts.Requests;
using Presentation.Contracts.Responses;
using Presentation.Services;

namespace Presentation.Controllers;

[Authorize]
[Route("api/v1/users")]
public sealed class UsersController(IFinancialApiService service) : ApiControllerBase
{
    /// <summary>Obtiene el perfil del usuario autenticado.</summary>
    [HttpGet("me", Name = "GetUserProfile")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public Task<UserResponse> GetMe(CancellationToken cancellationToken) =>
        service.GetCurrentUserAsync(CurrentUserId, cancellationToken);

    /// <summary>Actualiza el nombre visible del usuario autenticado.</summary>
    [HttpPut("me", Name = "UpdateUserProfile")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public Task<UserResponse> UpdateMe(
        UpdateUserProfileRequest request, CancellationToken cancellationToken) =>
        service.UpdateCurrentUserAsync(CurrentUserId, request, cancellationToken);

    /// <summary>Lista usuarios; requiere permiso administrativo.</summary>
    [Authorize(Policy = Permissions.UsersManage)]
    [HttpGet(Name = "ListUsers")]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<UserResponse>> List(CancellationToken cancellationToken) =>
        service.ListUsersAsync(cancellationToken);
}
