using Microsoft.AspNetCore.Mvc;
using Pharmacy.Api.Auth;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;
using Pharmacy.Core.Stocks;

namespace Pharmacy.Api.Controllers;

[ApiController]
[Route("api/stocks")]
public sealed class StocksController(StockService stockService) : ControllerBase
{
    /// <summary>Existence of the products of a branch, filtered by category.</summary>
    [HttpGet]
    [HasPermission(Permissions.PurchasesView)]
    public Task<PagedResult<StockListItemDto>> List([FromQuery] StockQuery query, CancellationToken ct) =>
        stockService.ListAsync(query, ct);
}
