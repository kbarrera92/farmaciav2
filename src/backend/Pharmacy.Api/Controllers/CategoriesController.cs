using Microsoft.AspNetCore.Mvc;
using Pharmacy.Api.Auth;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Categories;
using Pharmacy.Core.Common;

namespace Pharmacy.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(CategoryService categoryService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.CatalogView)]
    public Task<IReadOnlyList<CategoryDto>> List([FromQuery] bool includeInactive, CancellationToken ct) =>
        categoryService.ListAsync(includeInactive, ct);

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.CatalogView)]
    public Task<CategoryDto> Get(int id, CancellationToken ct) => categoryService.GetAsync(id, ct);

    [HttpPost]
    [HasPermission(Permissions.CatalogManage)]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CategoryDto>> Create(SaveCategoryRequest request, CancellationToken ct)
    {
        var category = await categoryService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = category.Id }, category);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.CatalogManage)]
    public Task<CategoryDto> Update(int id, SaveCategoryRequest request, CancellationToken ct) =>
        categoryService.UpdateAsync(id, request, ct);

    [HttpPatch("{id:int}/status")]
    [HasPermission(Permissions.CatalogManage)]
    public Task<CategoryDto> SetStatus(int id, SetStatusRequest request, CancellationToken ct) =>
        categoryService.SetStatusAsync(id, request.IsActive!.Value, ct);
}
