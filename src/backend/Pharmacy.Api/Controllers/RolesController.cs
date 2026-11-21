using Microsoft.AspNetCore.Mvc;
using Pharmacy.Api.Auth;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;
using Pharmacy.Core.Roles;

namespace Pharmacy.Api.Controllers;

[ApiController]
[Route("api/roles")]
public sealed class RolesController(RoleService roleService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.RolesView)]
    public Task<IReadOnlyList<RoleListItemDto>> List(CancellationToken ct) => roleService.ListAsync(ct);

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.RolesView)]
    public Task<RoleDetailDto> Get(int id, CancellationToken ct) => roleService.GetAsync(id, ct);

    [HttpPost]
    [HasPermission(Permissions.RolesManage)]
    [ProducesResponseType<RoleDetailDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RoleDetailDto>> Create(SaveRoleRequest request, CancellationToken ct)
    {
        var role = await roleService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = role.Id }, role);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.RolesManage)]
    public Task<RoleDetailDto> Update(int id, SaveRoleRequest request, CancellationToken ct) =>
        roleService.UpdateAsync(id, request, ct);

    [HttpPatch("{id:int}/status")]
    [HasPermission(Permissions.RolesManage)]
    public Task<RoleDetailDto> SetStatus(int id, SetStatusRequest request, CancellationToken ct) =>
        roleService.SetStatusAsync(id, request.IsActive!.Value, ct);

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.RolesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await roleService.DeleteAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/permissions")]
public sealed class PermissionsController : ControllerBase
{
    /// <summary>Catalog of assignable permissions grouped by module.</summary>
    [HttpGet]
    [HasPermission(Permissions.RolesView)]
    public IReadOnlyList<PermissionGroupDto> List() => RoleService.GetPermissionCatalog();
}
