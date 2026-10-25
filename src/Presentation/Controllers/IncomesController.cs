using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Contracts.Requests;
using Presentation.Contracts.Responses;
using Presentation.Services;

namespace Presentation.Controllers;

[Authorize]
[Route("api/v1/households/{householdId:guid}/incomes")]
public sealed class IncomesController(IFinancialApiService service) : ApiControllerBase
{
    /// <summary>Lista ingresos del hogar con filtros de fecha opcionales.</summary>
    [HttpGet(Name = "ListIncomes")]
    [ProducesResponseType(typeof(IReadOnlyList<IncomeResponse>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<IncomeResponse>> List(
        Guid householdId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        service.ListIncomesAsync(CurrentUserId, householdId, from, to, cancellationToken);

    /// <summary>Obtiene un ingreso del hogar.</summary>
    [HttpGet("{incomeId:guid}", Name = "GetIncome")]
    [ProducesResponseType(typeof(IncomeResponse), StatusCodes.Status200OK)]
    public Task<IncomeResponse> Get(Guid householdId, Guid incomeId, CancellationToken cancellationToken) =>
        service.GetIncomeAsync(CurrentUserId, householdId, incomeId, cancellationToken);

    /// <summary>Registra un ingreso compartido.</summary>
    [HttpPost(Name = "CreateIncome")]
    [ProducesResponseType(typeof(IncomeResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<IncomeResponse>> Create(
        Guid householdId, SaveIncomeRequest request, CancellationToken cancellationToken)
    {
        var income = await service.CreateIncomeAsync(CurrentUserId, householdId, request, cancellationToken);
        return CreatedAtRoute("GetIncome", new { householdId, incomeId = income.Id }, income);
    }

    /// <summary>Actualiza un ingreso propio o gestionado por un administrador.</summary>
    [HttpPut("{incomeId:guid}", Name = "UpdateIncome")]
    [ProducesResponseType(typeof(IncomeResponse), StatusCodes.Status200OK)]
    public Task<IncomeResponse> Update(
        Guid householdId, Guid incomeId, SaveIncomeRequest request, CancellationToken cancellationToken) =>
        service.UpdateIncomeAsync(CurrentUserId, householdId, incomeId, request, cancellationToken);

    /// <summary>Elimina lógicamente un ingreso propio o gestionado por un administrador.</summary>
    [HttpDelete("{incomeId:guid}", Name = "DeleteIncome")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid householdId, Guid incomeId, CancellationToken cancellationToken)
    {
        await service.DeleteIncomeAsync(CurrentUserId, householdId, incomeId, cancellationToken);
        return NoContent();
    }
}
