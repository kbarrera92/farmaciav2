namespace Pharmacy.Core.Auth;

/// <summary>Login lockout settings (section "Security:Lockout").</summary>
public sealed class LoginLockoutOptions
{
    public const string SectionName = "Security:Lockout";

    public int MaxFailedAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
}

/// <summary>Password rules (section "Security:Password").</summary>
public sealed class PasswordPolicyOptions
{
    public const string SectionName = "Security:Password";

    public int MinLength { get; set; } = 8;
    public bool RequireLetter { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
}
