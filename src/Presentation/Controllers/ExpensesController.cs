using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Contracts.Requests;
using Presentation.Contracts.Responses;
using Presentation.Services;

namespace Presentation.Controllers;

[Authorize]
[Route("api/v1/households/{householdId:guid}/expenses")]
public sealed class ExpensesController(IFinancialApiService service) : ApiControllerBase
{
    /// <summary>Lista gastos del hogar con filtros de fecha opcionales.</summary>
    [HttpGet(Name = "ListExpenses")]
    [ProducesResponseType(typeof(IReadOnlyList<ExpenseResponse>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<ExpenseResponse>> List(
        Guid householdId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        service.ListExpensesAsync(CurrentUserId, householdId, from, to, cancellationToken);

    /// <summary>Obtiene un gasto del hogar.</summary>
    [HttpGet("{expenseId:guid}", Name = "GetExpense")]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status200OK)]
    public Task<ExpenseResponse> Get(Guid householdId, Guid expenseId, CancellationToken cancellationToken) =>
        service.GetExpenseAsync(CurrentUserId, householdId, expenseId, cancellationToken);

    /// <summary>Registra un gasto compartido.</summary>
    [HttpPost(Name = "CreateExpense")]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ExpenseResponse>> Create(
        Guid householdId, SaveExpenseRequest request, CancellationToken cancellationToken)
    {
        var expense = await service.CreateExpenseAsync(CurrentUserId, householdId, request, cancellationToken);
        return CreatedAtRoute("GetExpense", new { householdId, expenseId = expense.Id }, expense);
    }

    /// <summary>Actualiza un gasto propio o gestionado por un administrador.</summary>
    [HttpPut("{expenseId:guid}", Name = "UpdateExpense")]
    [ProducesResponseType(typeof(ExpenseResponse), StatusCodes.Status200OK)]
    public Task<ExpenseResponse> Update(
        Guid householdId, Guid expenseId, SaveExpenseRequest request, CancellationToken cancellationToken) =>
        service.UpdateExpenseAsync(CurrentUserId, householdId, expenseId, request, cancellationToken);

    /// <summary>Elimina lógicamente un gasto propio o gestionado por un administrador.</summary>
    [HttpDelete("{expenseId:guid}", Name = "DeleteExpense")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid householdId, Guid expenseId, CancellationToken cancellationToken)
    {
        await service.DeleteExpenseAsync(CurrentUserId, householdId, expenseId, cancellationToken);
        return NoContent();
    }
}
