using System.Net;
using System.Net.Http.Json;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Categories;
using Pharmacy.Core.Common;
using Pharmacy.Core.Products;
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

    [Fact]
    public async Task Product_lifecycle_through_the_api()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var (categoryId, supplierId) = await CreateReferencesAsync(admin);
        var branches = (await admin.GetFromJsonAsync<List<NamedReference>>("/api/lookups/branches"))!;

        var create = await admin.PostAsJsonAsync("/api/products", new
        {
            name = "acetaminofén 500 mg",
            presentation = "Tabletas",
            barcode = "7501",
            categoryId,
            supplierId,
            price = 15.5m,
            cost = 10m,
            minStock = 5,
            isControlled = true,
            branches = new[] { new { branchId = branches[0].Id, price = 18m, minStock = 2, shelf = "A-3" } },
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var product = (await create.Content.ReadFromJsonAsync<ProductDetailDto>())!;
        Assert.Equal("Acetaminofén 500 mg", product.Name);
        Assert.Equal("A-3", Assert.Single(product.Branches).Shelf);

        var duplicateBarcode = await admin.PostAsJsonAsync("/api/products",
            new { name = "Otro", presentation = "Jarabe", barcode = "7501", categoryId, supplierId, price = 15m, cost = 10m });
        Assert.Equal(HttpStatusCode.Conflict, duplicateBarcode.StatusCode);

        var invalidPrice = await admin.PostAsJsonAsync("/api/products",
            new { name = "Otro", presentation = "Jarabe", categoryId, supplierId, price = 10m, cost = 10m });
        Assert.Equal(HttpStatusCode.BadRequest, invalidPrice.StatusCode);
        Assert.Contains("\"price\"", await invalidPrice.Content.ReadAsStringAsync());

        var missingName = await admin.PostAsJsonAsync("/api/products",
            new { name = "", presentation = "Jarabe", categoryId, supplierId, price = 15m, cost = 10m });
        Assert.Equal(HttpStatusCode.BadRequest, missingName.StatusCode);

        var page = (await admin.GetFromJsonAsync<PagedResult<ProductListItemDto>>("/api/products?isControlled=true"))!;
        Assert.Equal(1, page.TotalCount);
        Assert.Equal("Droguería Norte", page.Items[0].Supplier.Name);

        (await admin.PatchAsJsonAsync($"/api/products/{product.Id}/status", new { isActive = false })).EnsureSuccessStatusCode();
        Assert.Equal(0, (await admin.GetFromJsonAsync<PagedResult<ProductListItemDto>>("/api/products?isActive=true"))!.TotalCount);
    }

    [Fact]
    public async Task Catalog_view_can_read_products_but_not_change_them()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var (categoryId, supplierId) = await CreateReferencesAsync(admin);
        var product = await (await admin.PostAsJsonAsync("/api/products",
                new { name = "Ibuprofeno", presentation = "Tabletas", categoryId, supplierId, price = 15m, cost = 10m }))
            .Content.ReadFromJsonAsync<ProductDetailDto>();
        var (viewer, _) = await _factory.CreateUserClientAsync(admin, "consultaprod", Permissions.CatalogView);

        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/products")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"/api/products/{product!.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.PostAsJsonAsync("/api/products",
                new { name = "Otro", presentation = "X", categoryId, supplierId, price = 15m, cost = 10m })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.PatchAsJsonAsync($"/api/products/{product.Id}/status", new { isActive = false })).StatusCode);
    }

    private static async Task<(int CategoryId, int SupplierId)> CreateReferencesAsync(HttpClient admin)
    {
        var category = await (await admin.PostAsJsonAsync("/api/categories", new { name = "Analgésicos" }))
            .Content.ReadFromJsonAsync<CategoryDto>();
        var supplier = await (await admin.PostAsJsonAsync("/api/suppliers", new { name = "Droguería Norte" }))
            .Content.ReadFromJsonAsync<SupplierDto>();
        return (category!.Id, supplier!.Id);
    }
}
