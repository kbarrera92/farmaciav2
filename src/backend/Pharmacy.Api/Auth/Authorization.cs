using Microsoft.AspNetCore.Authorization;
using Pharmacy.Core.Auth;

namespace Pharmacy.Api.Auth;

public static class AuthPolicies
{
    /// <summary>
    /// Authenticated user, even if a password change is pending. Only for the endpoints the user
    /// needs to complete that change (me, logout, change-password).
    /// </summary>
    public const string AllowPendingPasswordChange = nameof(AllowPendingPasswordChange);
}

/// <summary>Requires the authenticated user's role to grant the given permission.</summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

/// <summary>Usage: <c>[HasPermission(Permissions.UsersManage)]</c>. Combined with the default policy.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute, IAuthorizationRequirementData
{
    public string Permission { get; } = permission;

    public IEnumerable<IAuthorizationRequirement> GetRequirements() => [new PermissionRequirement(Permission)];
}

public sealed class PermissionAuthorizationHandler(UserAccessService userAccess)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var permissions = await userAccess.GetPermissionsAsync(context.User.GetUserId());
        if (permissions.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}

/// <summary>Blocks every endpoint (except those using <see cref="AuthPolicies.AllowPendingPasswordChange"/>)
/// while the user still has to change the password.</summary>
public sealed class PasswordChangedRequirement : IAuthorizationRequirement;

public sealed class PasswordChangedHandler : AuthorizationHandler<PasswordChangedRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PasswordChangedRequirement requirement)
    {
        if (context.User.FindFirst(AppClaims.MustChangePassword)?.Value == "false")
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
