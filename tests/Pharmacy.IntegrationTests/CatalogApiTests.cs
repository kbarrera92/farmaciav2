using System.Net;
using System.Net.Http.Json;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Categories;
using Pharmacy.Core.Suppliers;

namespace Pharmacy.IntegrationTests;

public sealed class CatalogApiTests : IDisposable
{
    private readonly PharmacyApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Category_lifecycle_through_the_api()
    {
        var admin = await _factory.CreateAdminClientAsync();

        var create = await admin.PostAsJsonAsync("/api/categories", new { name = "Analgésicos" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var category = (await create.Content.ReadFromJsonAsync<CategoryDto>())!;

        var duplicate = await admin.PostAsJsonAsync("/api/categories", new { name = " ANALGESICOS " });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var empty = await admin.PostAsJsonAsync("/api/categories", new { name = "" });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

        var updated = await (await admin.PutAsJsonAsync($"/api/categories/{category.Id}", new { name = "Analgésicos y antipiréticos" }))
            .Content.ReadFromJsonAsync<CategoryDto>();
        Assert.Equal("Analgésicos y antipiréticos", updated!.Name);

        (await admin.PatchAsJsonAsync($"/api/categories/{category.Id}/status", new { isActive = false })).EnsureSuccessStatusCode();
        Assert.Empty((await admin.GetFromJsonAsync<List<CategoryDto>>("/api/categories"))!);
        Assert.Single((await admin.GetFromJsonAsync<List<CategoryDto>>("/api/categories?includeInactive=true"))!);
    }

    [Fact]
    public async Task Catalog_view_can_read_categories_but_not_change_them()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var category = await (await admin.PostAsJsonAsync("/api/categories", new { name = "Vitaminas" }))
            .Content.ReadFromJsonAsync<CategoryDto>();
        var (viewer, _) = await _factory.CreateUserClientAsync(admin, "consulta", Permissions.CatalogView);

        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/categories")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"/api/categories/{category!.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.PostAsJsonAsync("/api/categories", new { name = "Otra" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.PutAsJsonAsync($"/api/categories/{category.Id}", new { name = "Otra" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.PatchAsJsonAsync($"/api/categories/{category.Id}/status", new { isActive = false })).StatusCode);
    }

    [Fact]
    public async Task Categories_require_the_catalog_view_permission()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var (other, _) = await _factory.CreateUserClientAsync(admin, "sinpermiso", Permissions.SettingsView);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync("/api/categories")).StatusCode);
    }

    [Fact]
    public async Task Supplier_lifecycle_through_the_api()
    {
        var admin = await _factory.CreateAdminClientAsync();

        var create = await admin.PostAsJsonAsync("/api/suppliers", new { name = "droguería norte", taxId = "1234567-8" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var supplier = (await create.Content.ReadFromJsonAsync<SupplierDto>())!;
        Assert.Equal("Droguería Norte", supplier.Name);

        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsJsonAsync("/api/suppliers", new { name = "DROGUERIA NORTE" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PostAsJsonAsync("/api/suppliers", new { name = "Otra", taxId = "1234567-8" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync("/api/suppliers", new { name = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PostAsJsonAsync("/api/suppliers", new { name = "Con correo malo", email = "no-es-correo" })).StatusCode);

        (await admin.PatchAsJsonAsync($"/api/suppliers/{supplier.Id}/status", new { isActive = false })).EnsureSuccessStatusCode();
        Assert.Empty((await admin.GetFromJsonAsync<List<SupplierDto>>("/api/suppliers"))!);
        Assert.Single((await admin.GetFromJsonAsync<List<SupplierDto>>("/api/suppliers?includeInactive=true"))!);
    }

    [Fact]
    public async Task Catalog_view_can_read_suppliers_but_not_change_them()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var supplier = await (await admin.PostAsJsonAsync("/api/suppliers", new { name = "Distribuidora Sur" }))
            .Content.ReadFromJsonAsync<SupplierDto>();
        var (viewer, _) = await _factory.CreateUserClientAsync(admin, "consultaprov", Permissions.CatalogView);

        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/suppliers")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.PostAsJsonAsync("/api/suppliers", new { name = "Otro" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.PatchAsJsonAsync($"/api/suppliers/{supplier!.Id}/status", new { isActive = false })).StatusCode);
    }
}
