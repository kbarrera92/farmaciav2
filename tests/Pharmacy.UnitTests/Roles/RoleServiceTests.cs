using Pharmacy.Core.Auth;
using Pharmacy.Core.Branches;
using Pharmacy.Core.Common;
using Pharmacy.Core.Roles;
using Pharmacy.UnitTests.TestSupport;

namespace Pharmacy.UnitTests.Roles;

public sealed class RoleServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public async Task Create_role_with_permissions()
    {
        var role = await _env.Roles.CreateAsync(new SaveRoleRequest
        {
            Name = " Bodeguero ",
            Permissions = [Permissions.BranchesView, Permissions.UsersView],
        });

        Assert.Equal("Bodeguero", role.Name);
        Assert.Equal([Permissions.BranchesView, Permissions.UsersView], role.Permissions);
    }

    [Fact]
    public async Task Unknown_permission_is_rejected()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _env.Roles.CreateAsync(
            new SaveRoleRequest { Name = "Bodeguero", Permissions = ["sales.delete-everything"] }));
    }

    [Fact]
    public async Task Role_name_must_be_unique_ignoring_case_and_accents()
    {
        await Assert.ThrowsAsync<ConflictException>(
            () => _env.Roles.CreateAsync(new SaveRoleRequest { Name = "VENDEDOR" }));
        await Assert.ThrowsAsync<ConflictException>(
            () => _env.Roles.CreateAsync(new SaveRoleRequest { Name = "véndedor" }));
    }

    [Fact]
    public async Task System_role_cannot_be_modified_deactivated_or_deleted()
    {
        var id = _env.AdminRole.Id;

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _env.Roles.UpdateAsync(id, new SaveRoleRequest { Name = "Otro" }));
        await Assert.ThrowsAsync<BusinessRuleException>(() => _env.Roles.SetStatusAsync(id, false));
        await Assert.ThrowsAsync<BusinessRuleException>(() => _env.Roles.DeleteAsync(id));
    }

    [Fact]
    public async Task Role_with_users_cannot_be_deleted_and_with_active_users_cannot_be_deactivated()
    {
        await _env.CreateUserAsync("vendedor1");

        await Assert.ThrowsAsync<BusinessRuleException>(() => _env.Roles.DeleteAsync(_env.SellerRole.Id));
        await Assert.ThrowsAsync<BusinessRuleException>(() => _env.Roles.SetStatusAsync(_env.SellerRole.Id, false));
    }

    [Fact]
    public async Task Updating_permissions_takes_effect_immediately_for_users()
    {
        var user = await _env.CreateUserAsync("vendedor1");
        Assert.DoesNotContain(Permissions.BranchesView, await _env.UserAccess.GetPermissionsAsync(user.Id));

        await _env.Roles.UpdateAsync(_env.SellerRole.Id, new SaveRoleRequest
        {
            Name = "Vendedor",
            Permissions = [Permissions.BranchesView],
        });

        var permissions = await _env.UserAccess.GetPermissionsAsync(user.Id);
        Assert.Equal([Permissions.BranchesView], permissions);
    }

    [Fact]
    public async Task Unused_role_can_be_deleted()
    {
        var role = await _env.Roles.CreateAsync(new SaveRoleRequest { Name = "Temporal" });

        await _env.Roles.DeleteAsync(role.Id);

        await Assert.ThrowsAsync<NotFoundException>(() => _env.Roles.GetAsync(role.Id));
    }

    [Fact]
    public async Task Branch_with_active_users_cannot_be_deactivated()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _env.Branches.SetStatusAsync(_env.MainBranch.Id, false));

        var empty = await _env.Branches.CreateAsync(new SaveBranchRequest { Name = "Norte", Address = "Zona 18" });
        var result = await _env.Branches.SetStatusAsync(empty.Id, false);
        Assert.False(result.IsActive);
    }
}
