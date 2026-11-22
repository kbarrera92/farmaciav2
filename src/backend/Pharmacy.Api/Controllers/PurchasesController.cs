using Microsoft.AspNetCore.Mvc;
using Pharmacy.Api.Auth;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;
using Pharmacy.Core.Purchases;

namespace Pharmacy.Api.Controllers;

[ApiController]
[Route("api/purchases")]
public sealed class PurchasesController(PurchaseService purchaseService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.PurchasesView)]
    public Task<PagedResult<PurchaseListItemDto>> List([FromQuery] PurchaseQuery query, CancellationToken ct) =>
        purchaseService.ListAsync(query, ct);

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.PurchasesView)]
    public Task<PurchaseDetailDto> Get(int id, CancellationToken ct) => purchaseService.GetAsync(id, ct);

    /// <summary>Products that can be purchased, with the existence and sale price of the branch.</summary>
    [HttpGet("products")]
    [HasPermission(Permissions.PurchasesCreate)]
    public Task<PagedResult<PurchaseProductDto>> ListProducts([FromQuery] PurchaseProductQuery query, CancellationToken ct) =>
        purchaseService.ListProductsAsync(query, ct);

    [HttpPost]
    [HasPermission(Permissions.PurchasesCreate)]
    [ProducesResponseType<PurchaseDetailDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PurchaseDetailDto>> Create(SavePurchaseRequest request, CancellationToken ct)
    {
        var purchase = await purchaseService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = purchase.Id }, purchase);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.PurchasesManage)]
    public Task<PurchaseDetailDto> Update(int id, SavePurchaseRequest request, CancellationToken ct) =>
        purchaseService.UpdateAsync(id, request, ct);

    [HttpPost("{id:int}/cancel")]
    [HasPermission(Permissions.PurchasesManage)]
    public Task<PurchaseDetailDto> Cancel(int id, CancelPurchaseRequest request, CancellationToken ct) =>
        purchaseService.CancelAsync(id, request.Reason, ct);
}
