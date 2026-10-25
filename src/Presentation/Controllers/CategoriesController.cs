using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Contracts.Requests;
using Presentation.Contracts.Responses;
using Presentation.Services;

namespace Presentation.Controllers;

[Authorize]
[Route("api/v1/households/{householdId:guid}/categories")]
public sealed class CategoriesController(IFinancialApiService service) : ApiControllerBase
{
    /// <summary>Lista categorías y subcategorías del hogar.</summary>
    [HttpGet(Name = "ListCategories")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryResponse>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CategoryResponse>> List(Guid householdId, CancellationToken cancellationToken) =>
        service.ListCategoriesAsync(CurrentUserId, householdId, cancellationToken);

    /// <summary>Crea una categoría o subcategoría.</summary>
    [HttpPost(Name = "CreateCategory")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CategoryResponse>> Create(
        Guid householdId, SaveCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await service.CreateCategoryAsync(CurrentUserId, householdId, request, cancellationToken);
        return CreatedAtRoute("ListCategories", new { householdId }, category);
    }

    /// <summary>Actualiza el nombre, padre y orden de una categoría.</summary>
    [HttpPut("{categoryId:guid}", Name = "UpdateCategory")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    public Task<CategoryResponse> Update(
        Guid householdId, Guid categoryId, SaveCategoryRequest request, CancellationToken cancellationToken) =>
        service.UpdateCategoryAsync(CurrentUserId, householdId, categoryId, request, cancellationToken);

    /// <summary>Desactiva una categoría conservando sus referencias históricas.</summary>
    [HttpDelete("{categoryId:guid}", Name = "DeactivateCategory")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(Guid householdId, Guid categoryId, CancellationToken cancellationToken)
    {
        await service.DeactivateCategoryAsync(CurrentUserId, householdId, categoryId, cancellationToken);
        return NoContent();
    }
}
