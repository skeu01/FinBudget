using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Contracts.Requests;
using Presentation.Contracts.Responses;
using Presentation.Services;

namespace Presentation.Controllers;

[Authorize]
[Route("api/v1/households")]
public sealed class HouseholdsController(IFinancialApiService service) : ApiControllerBase
{
    /// <summary>Lista los hogares del usuario autenticado.</summary>
    [HttpGet(Name = "ListHouseholds")]
    [ProducesResponseType(typeof(IReadOnlyList<HouseholdResponse>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<HouseholdResponse>> List(CancellationToken cancellationToken) =>
        service.ListHouseholdsAsync(CurrentUserId, cancellationToken);

    /// <summary>Obtiene un hogar al que pertenece el usuario.</summary>
    [HttpGet("{householdId:guid}", Name = "GetHousehold")]
    [ProducesResponseType(typeof(HouseholdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<HouseholdResponse> Get(Guid householdId, CancellationToken cancellationToken) =>
        service.GetHouseholdAsync(CurrentUserId, householdId, cancellationToken);

    /// <summary>Crea un hogar y asigna al creador el rol de propietario.</summary>
    [HttpPost(Name = "CreateHousehold")]
    [ProducesResponseType(typeof(HouseholdResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<HouseholdResponse>> Create(
        CreateHouseholdRequest request, CancellationToken cancellationToken)
    {
        var household = await service.CreateHouseholdAsync(CurrentUserId, request, cancellationToken);
        return CreatedAtRoute("GetHousehold", new { householdId = household.Id }, household);
    }

    /// <summary>Actualiza el nombre del hogar. Requiere rol de propietario o administrador.</summary>
    [HttpPut("{householdId:guid}", Name = "UpdateHousehold")]
    [ProducesResponseType(typeof(HouseholdResponse), StatusCodes.Status200OK)]
    public Task<HouseholdResponse> Update(
        Guid householdId, UpdateHouseholdRequest request, CancellationToken cancellationToken) =>
        service.UpdateHouseholdAsync(CurrentUserId, householdId, request, cancellationToken);

    /// <summary>Archiva el hogar. Requiere rol de propietario.</summary>
    [HttpDelete("{householdId:guid}", Name = "ArchiveHousehold")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Archive(Guid householdId, CancellationToken cancellationToken)
    {
        await service.ArchiveHouseholdAsync(CurrentUserId, householdId, cancellationToken);
        return NoContent();
    }
}
