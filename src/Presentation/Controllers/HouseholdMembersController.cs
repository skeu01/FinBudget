using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Contracts.Requests;
using Presentation.Contracts.Responses;
using Presentation.Services;

namespace Presentation.Controllers;

[Authorize]
[Route("api/v1/households/{householdId:guid}/members")]
public sealed class HouseholdMembersController(IFinancialApiService service) : ApiControllerBase
{
    /// <summary>Lista los miembros activos de un hogar.</summary>
    [HttpGet(Name = "ListHouseholdMembers")]
    [ProducesResponseType(typeof(IReadOnlyList<HouseholdMemberResponse>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<HouseholdMemberResponse>> List(Guid householdId, CancellationToken cancellationToken) =>
        service.ListMembersAsync(CurrentUserId, householdId, cancellationToken);

    /// <summary>Agrega un usuario existente como miembro. Requiere propietario o administrador.</summary>
    [HttpPost(Name = "AddHouseholdMember")]
    [ProducesResponseType(typeof(HouseholdMemberResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<HouseholdMemberResponse>> Add(
        Guid householdId, AddHouseholdMemberRequest request, CancellationToken cancellationToken)
    {
        var member = await service.AddMemberAsync(CurrentUserId, householdId, request, cancellationToken);
        return CreatedAtRoute("ListHouseholdMembers", new { householdId }, member);
    }

    /// <summary>Cambia el rol de un miembro activo.</summary>
    [HttpPut("{memberId:guid}", Name = "UpdateHouseholdMember")]
    [ProducesResponseType(typeof(HouseholdMemberResponse), StatusCodes.Status200OK)]
    public Task<HouseholdMemberResponse> Update(
        Guid householdId, Guid memberId, UpdateHouseholdMemberRequest request, CancellationToken cancellationToken) =>
        service.UpdateMemberAsync(CurrentUserId, householdId, memberId, request, cancellationToken);

    /// <summary>Retira un miembro activo del hogar.</summary>
    [HttpDelete("{memberId:guid}", Name = "RemoveHouseholdMember")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(Guid householdId, Guid memberId, CancellationToken cancellationToken)
    {
        await service.RemoveMemberAsync(CurrentUserId, householdId, memberId, cancellationToken);
        return NoContent();
    }
}
