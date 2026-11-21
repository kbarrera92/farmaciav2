using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pharmacy.Api.Auth;
using Pharmacy.Core.Auth;

namespace Pharmacy.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService, AuthCookieService authCookie) : ControllerBase
{
    public const string LoginRateLimitPolicy = "login";

    /// <summary>Validates the credentials and sets the authentication cookie.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(LoginRateLimitPolicy)]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserDto>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await authService.LoginAsync(request.Username, request.Password, ct);
        authCookie.SignIn(Response, user);
        return await authService.GetCurrentUserAsync(user.Id, ct);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        authCookie.SignOut(Response);
        return NoContent();
    }

    /// <summary>Current user, role, branch and permissions.</summary>
    [HttpGet("me")]
    [Authorize(Policy = AuthPolicies.AllowPendingPasswordChange)]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken ct) =>
        await authService.GetCurrentUserAsync(User.GetUserId(), ct);

    /// <summary>Changes the password and renews the authentication cookie (other sessions are closed).</summary>
    [HttpPost("change-password")]
    [Authorize(Policy = AuthPolicies.AllowPendingPasswordChange)]
    public async Task<ActionResult<CurrentUserDto>> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await authService.ChangePasswordAsync(User.GetUserId(), request.CurrentPassword, request.NewPassword, ct);
        authCookie.SignIn(Response, user);
        return await authService.GetCurrentUserAsync(user.Id, ct);
    }
}
