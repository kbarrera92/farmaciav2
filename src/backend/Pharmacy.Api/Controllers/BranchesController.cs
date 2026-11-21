using Microsoft.AspNetCore.Mvc;
using Pharmacy.Api.Auth;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Branches;
using Pharmacy.Core.Common;

namespace Pharmacy.Api.Controllers;

[ApiController]
[Route("api/branches")]
public sealed class BranchesController(BranchService branchService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.BranchesView)]
    public Task<IReadOnlyList<BranchDto>> List([FromQuery] bool includeInactive, CancellationToken ct) =>
        branchService.ListAsync(includeInactive, ct);

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.BranchesView)]
    public Task<BranchDto> Get(int id, CancellationToken ct) => branchService.GetAsync(id, ct);

    [HttpPost]
    [HasPermission(Permissions.BranchesManage)]
    [ProducesResponseType<BranchDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<BranchDto>> Create(SaveBranchRequest request, CancellationToken ct)
    {
        var branch = await branchService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = branch.Id }, branch);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.BranchesManage)]
    public Task<BranchDto> Update(int id, SaveBranchRequest request, CancellationToken ct) =>
        branchService.UpdateAsync(id, request, ct);

    [HttpPatch("{id:int}/status")]
    [HasPermission(Permissions.BranchesManage)]
    public Task<BranchDto> SetStatus(int id, SetStatusRequest request, CancellationToken ct) =>
        branchService.SetStatusAsync(id, request.IsActive!.Value, ct);
}
