using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;
using Pharmacy.UnitTests.TestSupport;

namespace Pharmacy.UnitTests.Auth;

public sealed class AuthServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public async Task Login_with_valid_credentials_is_case_insensitive_and_records_last_login()
    {
        var result = await _env.Auth.LoginAsync("ADMIN", TestEnvironment.AdminPassword);

        Assert.Equal(_env.Admin.Id, result.Id);
        var user = await _env.Db.Users.AsNoTracking().SingleAsync(u => u.Id == _env.Admin.Id);
        Assert.Equal(_env.Clock.Now.UtcDateTime, user.LastLoginAtUtc);
    }

    [Fact]
    public async Task Login_with_unknown_user_fails()
    {
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _env.Auth.LoginAsync("nobody", "whatever1"));
    }

    [Fact]
    public async Task Login_of_inactive_user_fails_even_with_correct_password()
    {
        var user = await _env.CreateUserAsync("vendedor1");
        await _env.Users.SetStatusAsync(user.Id, false, _env.Admin.Id);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _env.Auth.LoginAsync("vendedor1", "Clave1234"));
    }

    [Fact]
    public async Task Repeated_wrong_passwords_lock_the_account_until_lockout_expires()
    {
        for (var i = 0; i < 3; i++)
            await Assert.ThrowsAsync<AuthenticationFailedException>(() => _env.Auth.LoginAsync("admin", "wrong-pass1"));

        // Locked: even the right password is rejected.
        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => _env.Auth.LoginAsync("admin", TestEnvironment.AdminPassword));

        _env.Clock.Advance(TimeSpan.FromMinutes(16));
        var result = await _env.Auth.LoginAsync("admin", TestEnvironment.AdminPassword);
        Assert.Equal(_env.Admin.Id, result.Id);
    }

    [Fact]
    public async Task Successful_login_resets_failed_attempts()
    {
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _env.Auth.LoginAsync("admin", "wrong-pass1"));
        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _env.Auth.LoginAsync("admin", "wrong-pass1"));
        await _env.Auth.LoginAsync("admin", TestEnvironment.AdminPassword);

        var user = await _env.Db.Users.AsNoTracking().SingleAsync(u => u.Id == _env.Admin.Id);
        Assert.Equal(0, user.FailedLoginAttempts);
    }

    [Fact]
    public async Task New_users_must_change_password_and_change_clears_the_flag_and_rotates_stamp()
    {
        var created = await _env.CreateUserAsync("vendedor1");
        var login = await _env.Auth.LoginAsync("vendedor1", "Clave1234");
        Assert.True(login.MustChangePassword);

        var changed = await _env.Auth.ChangePasswordAsync(created.Id, "Clave1234", "NuevaClave99");

        Assert.False(changed.MustChangePassword);
        Assert.NotEqual(login.SecurityStamp, changed.SecurityStamp);
        await _env.Auth.LoginAsync("vendedor1", "NuevaClave99");
    }

    [Fact]
    public async Task Change_password_rejects_wrong_current_password_and_reused_password()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _env.Auth.ChangePasswordAsync(_env.Admin.Id, "incorrecta1", "NuevaClave99"));
        await Assert.ThrowsAsync<ValidationException>(
            () => _env.Auth.ChangePasswordAsync(_env.Admin.Id, TestEnvironment.AdminPassword, TestEnvironment.AdminPassword));
    }

    [Fact]
    public async Task Current_user_of_system_role_has_every_permission()
    {
        var me = await _env.Auth.GetCurrentUserAsync(_env.Admin.Id);

        Assert.Equal(Permissions.AllCodes.Order(), me.Permissions.Order());
        Assert.Equal("Central", me.Branch.Name);
    }
}
