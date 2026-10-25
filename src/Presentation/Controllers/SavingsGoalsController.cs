using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Contracts.Requests;
using Presentation.Contracts.Responses;
using Presentation.Services;

namespace Presentation.Controllers;

[Authorize]
[Route("api/v1/households/{householdId:guid}/savings-goals")]
public sealed class SavingsGoalsController(IFinancialApiService service) : ApiControllerBase
{
    /// <summary>Lista objetivos de ahorro y su progreso.</summary>
    [HttpGet(Name = "ListSavingsGoals")]
    [ProducesResponseType(typeof(IReadOnlyList<SavingsGoalResponse>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<SavingsGoalResponse>> List(Guid householdId, CancellationToken cancellationToken) =>
        service.ListSavingsGoalsAsync(CurrentUserId, householdId, cancellationToken);

    /// <summary>Crea un objetivo de ahorro.</summary>
    [HttpPost(Name = "CreateSavingsGoal")]
    [ProducesResponseType(typeof(SavingsGoalResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<SavingsGoalResponse>> Create(
        Guid householdId, SaveSavingsGoalRequest request, CancellationToken cancellationToken)
    {
        var goal = await service.CreateSavingsGoalAsync(CurrentUserId, householdId, request, cancellationToken);
        return CreatedAtRoute("ListSavingsGoals", new { householdId }, goal);
    }

    /// <summary>Actualiza un objetivo propio o gestionado por un administrador.</summary>
    [HttpPut("{goalId:guid}", Name = "UpdateSavingsGoal")]
    [ProducesResponseType(typeof(SavingsGoalResponse), StatusCodes.Status200OK)]
    public Task<SavingsGoalResponse> Update(
        Guid householdId, Guid goalId, SaveSavingsGoalRequest request, CancellationToken cancellationToken) =>
        service.UpdateSavingsGoalAsync(CurrentUserId, householdId, goalId, request, cancellationToken);

    /// <summary>Marca un objetivo como completado.</summary>
    [HttpPost("{goalId:guid}/complete", Name = "CompleteSavingsGoal")]
    [ProducesResponseType(typeof(SavingsGoalResponse), StatusCodes.Status200OK)]
    public Task<SavingsGoalResponse> Complete(Guid householdId, Guid goalId, CancellationToken cancellationToken) =>
        service.CompleteSavingsGoalAsync(CurrentUserId, householdId, goalId, cancellationToken);

    /// <summary>Cancel un objetivo activo.</summary>
    [HttpPost("{goalId:guid}/cancel", Name = "CancelSavingsGoal")]
    [ProducesResponseType(typeof(SavingsGoalResponse), StatusCodes.Status200OK)]
    public Task<SavingsGoalResponse> Cancel(Guid householdId, Guid goalId, CancellationToken cancellationToken) =>
        service.CancelSavingsGoalAsync(CurrentUserId, householdId, goalId, cancellationToken);

    /// <summary>Lista aportaciones de un objetivo.</summary>
    [HttpGet("{goalId:guid}/contributions", Name = "ListSavingsContributions")]
    [ProducesResponseType(typeof(IReadOnlyList<SavingsContributionResponse>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<SavingsContributionResponse>> ListContributions(
        Guid householdId, Guid goalId, CancellationToken cancellationToken) =>
        service.ListContributionsAsync(CurrentUserId, householdId, goalId, cancellationToken);

    /// <summary>Añade una aportación a un objetivo activo.</summary>
    [HttpPost("{goalId:guid}/contributions", Name = "AddSavingsContribution")]
    [ProducesResponseType(typeof(SavingsContributionResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<SavingsContributionResponse>> AddContribution(
        Guid householdId, Guid goalId, AddSavingsContributionRequest request, CancellationToken cancellationToken)
    {
        var contribution = await service.AddContributionAsync(CurrentUserId, householdId, goalId, request, cancellationToken);
        return CreatedAtRoute("ListSavingsContributions", new { householdId, goalId }, contribution);
    }
}
