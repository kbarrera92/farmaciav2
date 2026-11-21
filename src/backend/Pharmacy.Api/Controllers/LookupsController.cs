using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Api.Auth;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Branches;
using Pharmacy.Core.Roles;

namespace Pharmacy.Api.Controllers;

/// <summary>Short id/name lists used by selectors.</summary>
[ApiController]
[Route("api/lookups")]
public sealed class LookupsController(BranchService branchService, RoleService roleService) : ControllerBase
{
    /// <summary>Active branches. Any authenticated user (other modules need branch names).</summary>
    [HttpGet("branches")]
    [Authorize]
    public Task<IReadOnlyList<NamedReference>> Branches(CancellationToken ct) =>
        branchService.ListActiveLookupAsync(ct);

    /// <summary>Active roles, for the user form.</summary>
    [HttpGet("roles")]
    [HasPermission(Permissions.UsersManage)]
    public Task<IReadOnlyList<NamedReference>> Roles(CancellationToken ct) =>
        roleService.ListActiveLookupAsync(ct);
}
