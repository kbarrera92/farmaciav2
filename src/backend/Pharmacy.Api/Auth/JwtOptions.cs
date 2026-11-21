namespace Pharmacy.Api.Auth;

/// <summary>JWT settings (section "Jwt"). The key must come from user-secrets or environment variables.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Pharmacy.Api";
    public string Audience { get; set; } = "Pharmacy.Web";

    /// <summary>HMAC-SHA256 signing key, at least 32 characters.</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 480;
    public string CookieName { get; set; } = "pharmacy_auth";
}
