using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pharmacy.Core.Auth;

namespace Pharmacy.Api.Auth;

/// <summary>Issues the JWT and stores it in an HttpOnly cookie.</summary>
public sealed class AuthCookieService(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private readonly JwtOptions _options = options.Value;

    public void SignIn(HttpResponse response, AuthenticatedUser user)
    {
        var now = timeProvider.GetUtcNow();
        var expires = now.AddMinutes(_options.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(AppClaims.Subject, user.Id.ToString()),
            new(AppClaims.Name, user.Username),
            new(AppClaims.SecurityStamp, user.SecurityStamp.ToString()),
            new(AppClaims.MustChangePassword, user.MustChangePassword ? "true" : "false"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        response.Cookies.Append(_options.CookieName, new JwtSecurityTokenHandler().WriteToken(token), new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = expires,
            IsEssential = true,
        });
    }

    public void SignOut(HttpResponse response) =>
        response.Cookies.Delete(_options.CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
        });
}

public static class AppClaims
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string SecurityStamp = "stamp";
    public const string MustChangePassword = "mcp";
}

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(AppClaims.Subject), out var id)
            ? id
            : throw new InvalidOperationException("The authenticated principal has no valid user id.");
}
