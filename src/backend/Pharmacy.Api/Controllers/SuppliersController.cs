using Microsoft.AspNetCore.Mvc;
using Pharmacy.Api.Auth;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;
using Pharmacy.Core.Suppliers;

namespace Pharmacy.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
public sealed class SuppliersController(SupplierService supplierService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.CatalogView)]
    public Task<IReadOnlyList<SupplierDto>> List([FromQuery] bool includeInactive, CancellationToken ct) =>
        supplierService.ListAsync(includeInactive, ct);

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.CatalogView)]
    public Task<SupplierDto> Get(int id, CancellationToken ct) => supplierService.GetAsync(id, ct);

    [HttpPost]
    [HasPermission(Permissions.CatalogManage)]
    [ProducesResponseType<SupplierDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<SupplierDto>> Create(SaveSupplierRequest request, CancellationToken ct)
    {
        var supplier = await supplierService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = supplier.Id }, supplier);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.CatalogManage)]
    public Task<SupplierDto> Update(int id, SaveSupplierRequest request, CancellationToken ct) =>
        supplierService.UpdateAsync(id, request, ct);

    [HttpPatch("{id:int}/status")]
    [HasPermission(Permissions.CatalogManage)]
    public Task<SupplierDto> SetStatus(int id, SetStatusRequest request, CancellationToken ct) =>
        supplierService.SetStatusAsync(id, request.IsActive!.Value, ct);
}
