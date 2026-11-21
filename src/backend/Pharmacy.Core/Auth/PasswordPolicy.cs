using Microsoft.Extensions.Options;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.Auth;

public sealed class PasswordPolicy(IOptions<PasswordPolicyOptions> options)
{
    private const int MaxLength = 128;
    private readonly PasswordPolicyOptions _options = options.Value;

    /// <summary>Throws <see cref="ValidationException"/> when the password does not satisfy the policy.</summary>
    public void EnsureValid(string? password, string field = "password")
    {
        var errors = new List<string>();
        password ??= string.Empty;

        if (password.Length < _options.MinLength)
            errors.Add($"La contraseña debe tener al menos {_options.MinLength} caracteres.");
        if (password.Length > MaxLength)
            errors.Add($"La contraseña no puede superar {MaxLength} caracteres.");
        if (_options.RequireLetter && !password.Any(char.IsLetter))
            errors.Add("La contraseña debe contener al menos una letra.");
        if (_options.RequireDigit && !password.Any(char.IsDigit))
            errors.Add("La contraseña debe contener al menos un número.");

        if (errors.Count > 0)
            throw new ValidationException(new Dictionary<string, string[]> { [field] = [.. errors] });
    }
}
