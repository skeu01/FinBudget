using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Contracts.Requests;
using Presentation.Contracts.Responses;
using Presentation.Services;

namespace Presentation.Controllers;

[Authorize]
[Route("api/v1/households/{householdId:guid}/budgets")]
public sealed class BudgetsController(IFinancialApiService service) : ApiControllerBase
{
    /// <summary>Lista presupuestos para un mes.</summary>
    [HttpGet(Name = "ListBudgets")]
    [ProducesResponseType(typeof(IReadOnlyList<BudgetResponse>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<BudgetResponse>> List(
        Guid householdId, [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken) =>
        service.ListBudgetsAsync(CurrentUserId, householdId, year, month, cancellationToken);

    /// <summary>Crea un presupuesto mensual.</summary>
    [HttpPost(Name = "CreateBudget")]
    [ProducesResponseType(typeof(BudgetResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<BudgetResponse>> Create(
        Guid householdId, SaveBudgetRequest request, CancellationToken cancellationToken)
    {
        var budget = await service.CreateBudgetAsync(CurrentUserId, householdId, request, cancellationToken);
        return CreatedAtRoute("ListBudgets", new { householdId, year = budget.Year, month = budget.Month }, budget);
    }

    /// <summary>Actualiza importes y umbral del presupuesto.</summary>
    [HttpPut("{budgetId:guid}", Name = "UpdateBudget")]
    [ProducesResponseType(typeof(BudgetResponse), StatusCodes.Status200OK)]
    public Task<BudgetResponse> Update(
        Guid householdId, Guid budgetId, SaveBudgetRequest request, CancellationToken cancellationToken) =>
        service.UpdateBudgetAsync(CurrentUserId, householdId, budgetId, request, cancellationToken);

    /// <summary>Elimina un presupuesto.</summary>
    [HttpDelete("{budgetId:guid}", Name = "DeleteBudget")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid householdId, Guid budgetId, CancellationToken cancellationToken)
    {
        await service.DeleteBudgetAsync(CurrentUserId, householdId, budgetId, cancellationToken);
        return NoContent();
    }
}
