using Microsoft.Extensions.Options;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;

namespace Pharmacy.UnitTests.Auth;

public sealed class PasswordPolicyTests
{
    private readonly PasswordPolicy _policy = new(Options.Create(new PasswordPolicyOptions()));

    [Theory]
    [InlineData("")]
    [InlineData("abc1")]          // too short
    [InlineData("abcdefghij")]    // no digit
    [InlineData("1234567890")]    // no letter
    public void Rejects_passwords_that_do_not_meet_the_policy(string password)
    {
        Assert.Throws<ValidationException>(() => _policy.EnsureValid(password));
    }

    [Fact]
    public void Accepts_valid_password()
    {
        _policy.EnsureValid("Farmacia2026");
    }
}
