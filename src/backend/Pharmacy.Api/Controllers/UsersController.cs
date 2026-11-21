using Microsoft.AspNetCore.Mvc;
using Pharmacy.Api.Auth;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;
using Pharmacy.Core.Users;

namespace Pharmacy.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(UserService userService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.UsersView)]
    public Task<PagedResult<UserListItemDto>> List([FromQuery] UserQuery query, CancellationToken ct) =>
        userService.ListAsync(query, ct);

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.UsersView)]
    public Task<UserDetailDto> Get(int id, CancellationToken ct) => userService.GetAsync(id, ct);

    [HttpPost]
    [HasPermission(Permissions.UsersManage)]
    [ProducesResponseType<UserDetailDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserDetailDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await userService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.UsersManage)]
    public Task<UserDetailDto> Update(int id, UpdateUserRequest request, CancellationToken ct) =>
        userService.UpdateAsync(id, request, ct);

    [HttpPatch("{id:int}/status")]
    [HasPermission(Permissions.UsersManage)]
    public Task<UserDetailDto> SetStatus(int id, SetStatusRequest request, CancellationToken ct) =>
        userService.SetStatusAsync(id, request.IsActive!.Value, User.GetUserId(), ct);

    /// <summary>Sets a temporary password; the user must change it at the next login.</summary>
    [HttpPost("{id:int}/reset-password")]
    [HasPermission(Permissions.UsersManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request, CancellationToken ct)
    {
        await userService.ResetPasswordAsync(id, request.NewPassword, ct);
        return NoContent();
    }
}
