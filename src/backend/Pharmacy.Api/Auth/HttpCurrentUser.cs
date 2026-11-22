using System.Security.Claims;
using Pharmacy.Core.Common;

namespace Pharmacy.Api.Auth;

/// <summary>Current user taken from the authenticated principal of the HTTP request.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public int? UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
                return null;
            return int.TryParse(principal.FindFirstValue(AppClaims.Subject), out var id) ? id : null;
        }
    }
}
