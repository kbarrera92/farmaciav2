using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Branches;
using Pharmacy.Core.Common;
using Pharmacy.Core.Roles;
using Pharmacy.UnitTests.TestSupport;

namespace Pharmacy.UnitTests.Common;

public sealed class AuditFieldsTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public async Task Creation_records_the_current_user_and_time()
    {
        _env.CurrentUser.UserId = _env.Admin.Id;

        var branch = await _env.Branches.CreateAsync(new SaveBranchRequest { Name = "Norte", Address = "Zona 18" });

        Assert.Equal(_env.Clock.Now.UtcDateTime, branch.Audit.CreatedAtUtc);
        Assert.Equal("Administrador", branch.Audit.CreatedBy);
        Assert.Equal(branch.Audit.CreatedAtUtc, branch.Audit.UpdatedAtUtc);
        Assert.Equal("Administrador", branch.Audit.UpdatedBy);
    }

    [Fact]
    public async Task Update_records_the_modifier_and_keeps_the_creation_data()
    {
        _env.CurrentUser.UserId = _env.Admin.Id;
        var branch = await _env.Branches.CreateAsync(new SaveBranchRequest { Name = "Norte", Address = "Zona 18" });
        var created = branch.Audit.CreatedAtUtc;
        var other = await _env.CreateUserAsync("editor");

        _env.CurrentUser.UserId = other.Id;
        _env.Clock.Advance(TimeSpan.FromHours(2));
        var updated = await _env.Branches.UpdateAsync(branch.Id, new SaveBranchRequest { Name = "Norte 2", Address = "Zona 18" });

        Assert.Equal(created, updated.Audit.CreatedAtUtc);
        Assert.Equal("Administrador", updated.Audit.CreatedBy);
        Assert.Equal(_env.Clock.Now.UtcDateTime, updated.Audit.UpdatedAtUtc);
        Assert.Equal("Usuario editor", updated.Audit.UpdatedBy);
    }

    [Fact]
    public async Task Creation_data_cannot_be_overwritten()
    {
        _env.CurrentUser.UserId = _env.Admin.Id;
        var branch = await _env.Branches.CreateAsync(new SaveBranchRequest { Name = "Norte", Address = "Zona 18" });
        _env.Db.ChangeTracker.Clear();

        var entity = await _env.Db.Branches.SingleAsync(b => b.Id == branch.Id);
        entity.CreatedAtUtc = new DateTime(2000, 1, 1);
        entity.CreatedByUserId = null;
        await _env.Db.SaveChangesAsync();
        _env.Db.ChangeTracker.Clear();

        var reloaded = await _env.Branches.GetAsync(branch.Id);
        Assert.Equal(branch.Audit.CreatedAtUtc, reloaded.Audit.CreatedAtUtc);
        Assert.Equal("Administrador", reloaded.Audit.CreatedBy);
    }

    [Fact]
    public async Task Changes_without_a_user_are_attributed_to_the_system()
    {
        var branch = await _env.Branches.CreateAsync(new SaveBranchRequest { Name = "Norte", Address = "Zona 18" });

        Assert.Null(branch.Audit.CreatedBy);
        Assert.Null(branch.Audit.UpdatedBy);
    }

    [Fact]
    public async Task Login_bookkeeping_does_not_count_as_a_modification()
    {
        var before = await _env.Users.GetAsync(_env.Admin.Id);
        _env.Clock.Advance(TimeSpan.FromHours(1));

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => _env.Auth.LoginAsync("admin", "incorrecta1"));
        await _env.Auth.LoginAsync("admin", TestEnvironment.AdminPassword);

        var after = await _env.Users.GetAsync(_env.Admin.Id);
        Assert.NotNull(after.LastLoginAtUtc);
        Assert.Equal(before.Audit.UpdatedAtUtc, after.Audit.UpdatedAtUtc);
    }

    [Fact]
    public async Task Changing_only_the_permissions_of_a_role_marks_the_role_as_modified()
    {
        var role = await _env.Roles.CreateAsync(new SaveRoleRequest { Name = "Bodeguero", Permissions = [Permissions.BranchesView] });
        _env.Db.ChangeTracker.Clear();

        _env.CurrentUser.UserId = _env.Admin.Id;
        _env.Clock.Advance(TimeSpan.FromHours(1));
        var updated = await _env.Roles.UpdateAsync(role.Id,
            new SaveRoleRequest { Name = "Bodeguero", Permissions = [Permissions.BranchesView, Permissions.UsersView] });

        Assert.Equal(_env.Clock.Now.UtcDateTime, updated.Audit.UpdatedAtUtc);
        Assert.Equal("Administrador", updated.Audit.UpdatedBy);
        Assert.Null(updated.Audit.CreatedBy);
    }
}
