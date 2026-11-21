using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Branches;
using Pharmacy.Core.Common;
using Pharmacy.Core.Roles;
using Pharmacy.Core.Users;
using Pharmacy.UnitTests.TestSupport;

namespace Pharmacy.UnitTests.Users;

public sealed class UserServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public async Task Create_stores_hashed_password_and_requires_password_change()
    {
        var created = await _env.CreateUserAsync("vendedor1");

        var user = await _env.Db.Users.AsNoTracking().SingleAsync(u => u.Id == created.Id);
        Assert.NotEqual("Clave1234", user.PasswordHash);
        Assert.True(user.MustChangePassword);
        Assert.Equal("VENDEDOR1", user.NormalizedUsername);
    }

    [Fact]
    public async Task Username_must_be_unique_ignoring_case()
    {
        await _env.CreateUserAsync("vendedor1");

        await Assert.ThrowsAsync<ConflictException>(() => _env.CreateUserAsync("VENDEDOR1"));
    }

    [Fact]
    public async Task Cannot_assign_inactive_role_or_branch()
    {
        var role = await _env.Roles.CreateAsync(new SaveRoleRequest { Name = "Bodeguero" });
        await _env.Roles.SetStatusAsync(role.Id, false);
        await Assert.ThrowsAsync<ValidationException>(() => _env.CreateUserAsync("bodega1", role.Id));

        var branch = await _env.Branches.CreateAsync(new SaveBranchRequest { Name = "Norte", Address = "Zona 18" });
        await _env.Branches.SetStatusAsync(branch.Id, false);
        await Assert.ThrowsAsync<ValidationException>(() => _env.Users.CreateAsync(new CreateUserRequest
        {
            FullName = "X", Username = "x1", Password = "Clave1234", RoleId = _env.SellerRole.Id, BranchId = branch.Id,
        }));
    }

    [Fact]
    public async Task Weak_password_is_rejected()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _env.CreateUserAsync("vendedor1", password: "corta"));
    }

    [Fact]
    public async Task Cannot_deactivate_yourself_or_the_last_active_administrator()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _env.Users.SetStatusAsync(_env.Admin.Id, false, _env.Admin.Id));

        var other = await _env.CreateUserAsync("vendedor1");
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _env.Users.SetStatusAsync(_env.Admin.Id, false, other.Id));
    }

    [Fact]
    public async Task Administrator_can_be_deactivated_when_another_active_administrator_exists()
    {
        var secondAdmin = await _env.CreateUserAsync("admin2", _env.AdminRole.Id);

        var result = await _env.Users.SetStatusAsync(_env.Admin.Id, false, secondAdmin.Id);

        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task Cannot_change_role_of_the_last_active_administrator()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _env.Users.UpdateAsync(_env.Admin.Id, new UpdateUserRequest
        {
            FullName = "Administrador", Username = "admin", RoleId = _env.SellerRole.Id, BranchId = _env.MainBranch.Id,
        }));
    }

    [Fact]
    public async Task Deactivation_rotates_the_security_stamp()
    {
        var created = await _env.CreateUserAsync("vendedor1");
        var before = (await _env.UserAccess.GetUserStateAsync(created.Id))!.SecurityStamp;

        await _env.Users.SetStatusAsync(created.Id, false, _env.Admin.Id);

        var after = await _env.UserAccess.GetUserStateAsync(created.Id);
        Assert.False(after!.IsActive);
        Assert.NotEqual(before, after.SecurityStamp);
    }

    [Fact]
    public async Task Reset_password_unlocks_and_requires_password_change()
    {
        var created = await _env.CreateUserAsync("vendedor1");
        await _env.Auth.ChangePasswordAsync(created.Id, "Clave1234", "NuevaClave99");
        for (var i = 0; i < 3; i++)
            await Assert.ThrowsAsync<AuthenticationFailedException>(() => _env.Auth.LoginAsync("vendedor1", "mala-clave1"));

        await _env.Users.ResetPasswordAsync(created.Id, "Temporal123");

        var login = await _env.Auth.LoginAsync("vendedor1", "Temporal123");
        Assert.True(login.MustChangePassword);
    }

    [Fact]
    public async Task List_filters_by_search_and_paginates()
    {
        await _env.CreateUserAsync("ana");
        await _env.CreateUserAsync("beto");
        await _env.CreateUserAsync("carla");

        var page = await _env.Users.ListAsync(new UserQuery { Search = "beto" });
        Assert.Single(page.Items);

        var firstPage = await _env.Users.ListAsync(new UserQuery { PageSize = 2 });
        Assert.Equal(4, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
    }

    [Fact]
    public async Task Role_permissions_apply_to_the_user()
    {
        var created = await _env.CreateUserAsync("vendedor1");

        var permissions = await _env.UserAccess.GetPermissionsAsync(created.Id);

        Assert.Equal([Permissions.UsersView], permissions);
    }
}
