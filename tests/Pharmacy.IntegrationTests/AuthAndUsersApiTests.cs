using System.Net;
using System.Net.Http.Json;
using Pharmacy.Core.Auth;

namespace Pharmacy.IntegrationTests;

public sealed class AuthAndUsersApiTests : IDisposable
{
    private readonly PharmacyApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Login_sets_an_HttpOnly_cookie_and_returns_the_current_user()
    {
        var client = _factory.CreateBrowserClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { username = "ADMIN", password = PharmacyApiFactory.AdminInitialPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("pharmacy_auth=", cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        var me = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.Equal("admin", me!.Username);
        Assert.True(me.MustChangePassword);
    }

    [Fact]
    public async Task Wrong_credentials_return_401_problem_details()
    {
        var client = _factory.CreateBrowserClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "incorrecta1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Invalid_login_payload_returns_400()
    {
        var client = _factory.CreateBrowserClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoints_require_authentication()
    {
        var client = _factory.CreateBrowserClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Pending_password_change_blocks_everything_except_the_change_itself()
    {
        var client = await _factory.LoginAsync(PharmacyApiFactory.AdminUsername, PharmacyApiFactory.AdminInitialPassword);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);

        var change = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = PharmacyApiFactory.AdminInitialPassword,
            newPassword = PharmacyApiFactory.AdminPassword,
        });
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);

        // The renewed cookie no longer has a pending change.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users")).StatusCode);
    }

    [Fact]
    public async Task Endpoints_require_the_matching_permission()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var (seller, _) = await _factory.CreateUserClientAsync(admin, "vendedor1", Permissions.UsersView);

        Assert.Equal(HttpStatusCode.OK, (await seller.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await seller.GetAsync("/api/roles")).StatusCode);

        var create = await seller.PostAsJsonAsync("/api/users", new
        {
            fullName = "X", username = "x1", password = "Clave1234", roleId = 1, branchId = 1,
        });
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task Permission_changes_on_the_role_apply_without_logging_in_again()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var (user, _) = await _factory.CreateUserClientAsync(admin, "bodega1");
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/branches")).StatusCode);

        var me = await user.GetFromJsonAsync<CurrentUserDto>("/api/auth/me");
        (await admin.PutAsJsonAsync($"/api/roles/{me!.Role.Id}",
            new { name = me.Role.Name, permissions = new[] { Permissions.BranchesView } })).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/branches")).StatusCode);
    }

    [Fact]
    public async Task Deactivating_or_resetting_a_user_closes_their_session()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var (user, userId) = await _factory.CreateUserClientAsync(admin, "vendedor1", Permissions.UsersView);
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/auth/me")).StatusCode);

        (await admin.PostAsJsonAsync($"/api/users/{userId}/reset-password", new { newPassword = "Reinicio123" }))
            .EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await user.GetAsync("/api/auth/me")).StatusCode);

        var relogged = await _factory.LoginAsync("vendedor1", "Reinicio123");
        (await admin.PatchAsJsonAsync($"/api/users/{userId}/status", new { isActive = false })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await relogged.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Business_rule_violations_return_422_and_conflicts_409()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var me = await admin.GetFromJsonAsync<CurrentUserDto>("/api/auth/me");

        var selfDeactivate = await admin.PatchAsJsonAsync($"/api/users/{me!.Id}/status", new { isActive = false });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, selfDeactivate.StatusCode);

        var duplicate = await admin.PostAsJsonAsync("/api/users", new
        {
            fullName = "Otro", username = "Admin", password = "Clave1234", roleId = me.Role.Id, branchId = me.Branch.Id,
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Logout_removes_the_cookie()
    {
        var admin = await _factory.CreateAdminClientAsync();

        (await admin.PostAsync("/api/auth/logout", null)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Unauthorized, (await admin.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Unknown_api_route_returns_404()
    {
        var admin = await _factory.CreateAdminClientAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/does-not-exist")).StatusCode);
    }
}
