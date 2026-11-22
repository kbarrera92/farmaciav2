using Microsoft.AspNetCore.Mvc;
using Pharmacy.Api.Auth;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;
using Pharmacy.Core.Products;

namespace Pharmacy.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ProductService productService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.CatalogView)]
    public Task<PagedResult<ProductListItemDto>> List([FromQuery] ProductQuery query, CancellationToken ct) =>
        productService.ListAsync(query, ct);

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.CatalogView)]
    public Task<ProductDetailDto> Get(int id, CancellationToken ct) => productService.GetAsync(id, ct);

    [HttpPost]
    [HasPermission(Permissions.CatalogManage)]
    [ProducesResponseType<ProductDetailDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductDetailDto>> Create(SaveProductRequest request, CancellationToken ct)
    {
        var product = await productService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.CatalogManage)]
    public Task<ProductDetailDto> Update(int id, SaveProductRequest request, CancellationToken ct) =>
        productService.UpdateAsync(id, request, ct);

    [HttpPatch("{id:int}/status")]
    [HasPermission(Permissions.CatalogManage)]
    public Task<ProductDetailDto> SetStatus(int id, SetStatusRequest request, CancellationToken ct) =>
        productService.SetStatusAsync(id, request.IsActive!.Value, ct);
}
