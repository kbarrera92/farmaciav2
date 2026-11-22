using System.Net;
using System.Net.Http.Json;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Categories;
using Pharmacy.Core.Common;
using Pharmacy.Core.PaymentMethods;
using Pharmacy.Core.Products;
using Pharmacy.Core.Purchases;
using Pharmacy.Core.Stocks;
using Pharmacy.Core.Suppliers;

namespace Pharmacy.IntegrationTests;

public sealed class PurchasesApiTests : IDisposable
{
    private readonly PharmacyApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Purchase_lifecycle_through_the_api()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var data = await CreateReferencesAsync(admin);

        var create = await admin.PostAsJsonAsync("/api/purchases", NewPurchase(data, "FAC-1"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var purchase = (await create.Content.ReadFromJsonAsync<PurchaseDetailDto>())!;
        Assert.Equal(PurchaseType.Credit, purchase.Type);
        Assert.Equal(60m, purchase.Total);
        Assert.NotNull(purchase.AccountPayable);
        Assert.Contains("\"type\":\"Credit\"", await admin.GetStringAsync($"/api/purchases/{purchase.Id}"));

        var duplicate = await admin.PostAsJsonAsync("/api/purchases", NewPurchase(data, "fac-1"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var noItems = await admin.PostAsJsonAsync("/api/purchases", new
        {
            purchaseDate = "2026-01-10", data.BranchId, data.SupplierId, documentNumber = "FAC-2",
            type = "Credit", creditDays = 15, items = Array.Empty<object>(),
        });
        Assert.Equal(HttpStatusCode.BadRequest, noItems.StatusCode);

        var products = (await admin.GetFromJsonAsync<PagedResult<PurchaseProductDto>>(
            $"/api/purchases/products?branchId={data.BranchId}"))!;
        Assert.Equal(6, Assert.Single(products.Items).Stock);

        var page = (await admin.GetFromJsonAsync<PagedResult<PurchaseListItemDto>>("/api/purchases?type=Credit"))!;
        Assert.Equal(1, page.TotalCount);

        var noReason = await admin.PostAsJsonAsync($"/api/purchases/{purchase.Id}/cancel", new { reason = "" });
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);

        var cancel = await admin.PostAsJsonAsync($"/api/purchases/{purchase.Id}/cancel", new { reason = "Factura duplicada" });
        cancel.EnsureSuccessStatusCode();
        Assert.True((await cancel.Content.ReadFromJsonAsync<PurchaseDetailDto>())!.IsCancelled);

        var again = await admin.PostAsJsonAsync($"/api/purchases/{purchase.Id}/cancel", new { reason = "Otra vez" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/purchases/9999")).StatusCode);
    }

    [Fact]
    public async Task Each_endpoint_requires_its_own_permission()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var data = await CreateReferencesAsync(admin);
        var purchase = await (await admin.PostAsJsonAsync("/api/purchases", NewPurchase(data, "FAC-1")))
            .Content.ReadFromJsonAsync<PurchaseDetailDto>();

        var (viewer, _) = await _factory.CreateUserClientAsync(admin, "comprasver", Permissions.PurchasesView);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/purchases")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"/api/purchases/{purchase!.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.PostAsJsonAsync("/api/purchases", NewPurchase(data, "FAC-2"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.GetAsync($"/api/purchases/products?branchId={data.BranchId}")).StatusCode);

        var (creator, _) = await _factory.CreateUserClientAsync(admin, "comprascrear", Permissions.PurchasesCreate);
        Assert.Equal(HttpStatusCode.Created,
            (await creator.PostAsJsonAsync("/api/purchases", NewPurchase(data, "FAC-3"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await creator.GetAsync($"/api/purchases/products?branchId={data.BranchId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await creator.GetAsync("/api/purchases")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await creator.PutAsJsonAsync($"/api/purchases/{purchase.Id}", NewPurchase(data, "FAC-1"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await creator.PostAsJsonAsync($"/api/purchases/{purchase.Id}/cancel", new { reason = "x" })).StatusCode);

        var (manager, _) = await _factory.CreateUserClientAsync(admin, "comprasadmin", Permissions.PurchasesManage);
        Assert.Equal(HttpStatusCode.OK,
            (await manager.PutAsJsonAsync($"/api/purchases/{purchase.Id}", NewPurchase(data, "FAC-1"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await manager.PostAsJsonAsync($"/api/purchases/{purchase.Id}/cancel", new { reason = "Error" })).StatusCode);
    }

    [Fact]
    public async Task Stocks_show_the_purchased_existence_and_require_purchases_view()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var data = await CreateReferencesAsync(admin);
        (await admin.PostAsJsonAsync("/api/purchases", NewPurchase(data, "FAC-1"))).EnsureSuccessStatusCode();

        var stocks = (await admin.GetFromJsonAsync<PagedResult<StockListItemDto>>(
            $"/api/stocks?branchId={data.BranchId}"))!;
        Assert.Equal(6, Assert.Single(stocks.Items).Quantity);

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/stocks")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/stocks?branchId=9999")).StatusCode);

        var (other, _) = await _factory.CreateUserClientAsync(admin, "sinexistencias", Permissions.CatalogView);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/stocks?branchId={data.BranchId}")).StatusCode);
        var (viewer, _) = await _factory.CreateUserClientAsync(admin, "conexistencias", Permissions.PurchasesView);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"/api/stocks?branchId={data.BranchId}")).StatusCode);
    }

    private static object NewPurchase(TestData data, string document) => new
    {
        purchaseDate = "2026-01-10",
        data.BranchId,
        data.SupplierId,
        documentNumber = document,
        type = "Credit",
        creditDays = 15,
        items = new[] { new { data.ProductId, quantity = 6, unitCost = 10m } },
    };

    private async Task<TestData> CreateReferencesAsync(HttpClient admin)
    {
        var category = await (await admin.PostAsJsonAsync("/api/categories", new { name = "Analgésicos" }))
            .Content.ReadFromJsonAsync<CategoryDto>();
        var supplier = await (await admin.PostAsJsonAsync("/api/suppliers", new { name = "Droguería Norte" }))
            .Content.ReadFromJsonAsync<SupplierDto>();
        var product = await (await admin.PostAsJsonAsync("/api/products", new
        {
            name = "Aspirina", presentation = "Tabletas", categoryId = category!.Id, supplierId = supplier!.Id,
            price = 15m, cost = 10m,
        })).Content.ReadFromJsonAsync<ProductDetailDto>();
        var branches = await admin.GetFromJsonAsync<List<NamedReference>>("/api/lookups/branches");
        return new TestData(branches![0].Id, supplier.Id, product!.Id);
    }

    private sealed record TestData(int BranchId, int SupplierId, int ProductId);
}
