using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Pharmacy.Infrastructure.Persistence;

namespace Pharmacy.IntegrationTests;

/// <summary>Runs the real API against an isolated SQLite in-memory database with a seeded administrator.</summary>
public sealed class PharmacyApiFactory : WebApplicationFactory<Program>
{
    public const string AdminUsername = "admin";
    public const string AdminInitialPassword = "Admin12345";
    public const string AdminPassword = "AdminNueva1";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public PharmacyApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Pharmacy", "not-used");
        builder.UseSetting("Jwt:Key", "integration-tests-signing-key-0123456789abcdef");
        builder.UseSetting("Database:SeedOnStartup", "false");
        builder.UseSetting("Security:LoginRateLimit:PermitLimit", "1000");
        builder.UseSetting("InitialAdmin:Username", AdminUsername);
        builder.UseSetting("InitialAdmin:Password", AdminInitialPassword);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AppDbContext>));
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        scope.ServiceProvider.GetRequiredService<DbInitializer>().SeedAsync().GetAwaiter().GetResult();
        return host;
    }

    public HttpClient CreateBrowserClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
            AllowAutoRedirect = false,
        });

    public async Task<HttpClient> LoginAsync(string username, string password)
    {
        var client = CreateBrowserClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        response.EnsureSuccessStatusCode();
        return client;
    }

    /// <summary>Administrator client with the initial password already changed.</summary>
    public async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = await LoginAsync(AdminUsername, AdminInitialPassword);
        var response = await client.PostAsJsonAsync("/api/auth/change-password",
            new { currentPassword = AdminInitialPassword, newPassword = AdminPassword });
        response.EnsureSuccessStatusCode();
        return client;
    }

    /// <summary>Creates a user with the given permissions and returns a client logged in as that user.</summary>
    public async Task<(HttpClient Client, int UserId)> CreateUserClientAsync(
        HttpClient admin, string username, params string[] permissions)
    {
        var role = await (await admin.PostAsJsonAsync("/api/roles",
            new { name = $"Rol {username}", permissions })).Content.ReadFromJsonAsync<IdResponse>();
        var branches = await admin.GetFromJsonAsync<List<IdResponse>>("/api/lookups/branches");

        var createUser = await admin.PostAsJsonAsync("/api/users", new
        {
            fullName = $"Usuario {username}",
            username,
            password = "Temporal123",
            roleId = role!.Id,
            branchId = branches![0].Id,
        });
        createUser.EnsureSuccessStatusCode();
        var user = await createUser.Content.ReadFromJsonAsync<IdResponse>();

        var client = await LoginAsync(username, "Temporal123");
        (await client.PostAsJsonAsync("/api/auth/change-password",
            new { currentPassword = "Temporal123", newPassword = "Definitiva123" })).EnsureSuccessStatusCode();

        return (client, user!.Id);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }

    public sealed record IdResponse(int Id);
}
