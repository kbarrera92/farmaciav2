using Pharmacy.Core.Branches;
using Pharmacy.Core.Categories;
using Pharmacy.Core.Common;
using Pharmacy.Core.PaymentMethods;
using Pharmacy.Core.Products;
using Pharmacy.Core.Purchases;
using Pharmacy.Core.Stocks;
using Pharmacy.Core.Suppliers;
using Pharmacy.UnitTests.TestSupport;

namespace Pharmacy.UnitTests.Stocks;

public sealed class StockServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly int _supplierId;
    private readonly int _painCategoryId;
    private readonly int _vitaminCategoryId;

    public StockServiceTests()
    {
        _painCategoryId = _env.Categories.CreateAsync(new SaveCategoryRequest { Name = "Analgésicos" }).Result.Id;
        _vitaminCategoryId = _env.Categories.CreateAsync(new SaveCategoryRequest { Name = "Vitaminas" }).Result.Id;
        _supplierId = _env.Suppliers.CreateAsync(new SaveSupplierRequest { Name = "Droguería Norte" }).Result.Id;
    }

    public void Dispose() => _env.Dispose();

    [Fact]
    public async Task Shows_the_branch_existence_with_zero_when_there_is_no_movement()
    {
        var aspirin = await CreateProduct("Aspirina", _painCategoryId, minStock: 5);
        var vitamin = await CreateProduct("Vitamina C", _vitaminCategoryId);
        await Buy(aspirin.Id, 12);

        var page = await _env.Stocks.ListAsync(new StockQuery { BranchId = _env.MainBranch.Id });

        Assert.Equal(2, page.TotalCount);
        var first = page.Items[0];
        Assert.Equal("Aspirina", first.Name);
        Assert.Equal("Analgésicos", first.Category.Name);
        Assert.Equal(12, first.Quantity);
        Assert.Equal(5, first.MinStock);
        Assert.Equal(0, page.Items.Single(i => i.ProductId == vitamin.Id).Quantity);
        Assert.Null(page.Items.Single(i => i.ProductId == vitamin.Id).MinStock);
    }

    [Fact]
    public async Task Uses_the_branch_minimum_stock_and_each_branch_has_its_own_existence()
    {
        var other = await _env.Branches.CreateAsync(new SaveBranchRequest { Name = "Norte", Address = "Zona 2" });
        var request = ProductRequest("Aspirina", _painCategoryId);
        request.MinStock = 5;
        request.Branches = [new ProductBranchRequest { BranchId = other.Id, MinStock = 20 }];
        var aspirin = await _env.Products.CreateAsync(request);
        await Buy(aspirin.Id, 12);

        var main = await _env.Stocks.ListAsync(new StockQuery { BranchId = _env.MainBranch.Id });
        var north = await _env.Stocks.ListAsync(new StockQuery { BranchId = other.Id });

        Assert.Equal((12, 5), (main.Items[0].Quantity, main.Items[0].MinStock));
        Assert.Equal((0, 20), (north.Items[0].Quantity, north.Items[0].MinStock));
    }

    [Fact]
    public async Task Filters_by_category_and_search_and_rejects_an_unknown_branch()
    {
        await CreateProduct("Aspirina", _painCategoryId);
        var vitamin = await CreateProduct("Vitamina C", _vitaminCategoryId);

        var byCategory = await _env.Stocks.ListAsync(
            new StockQuery { BranchId = _env.MainBranch.Id, CategoryId = _vitaminCategoryId });
        Assert.Equal(vitamin.Id, Assert.Single(byCategory.Items).ProductId);

        var bySearch = await _env.Stocks.ListAsync(new StockQuery { BranchId = _env.MainBranch.Id, Search = "Aspir" });
        Assert.Equal("Aspirina", Assert.Single(bySearch.Items).Name);

        await Assert.ThrowsAsync<NotFoundException>(() => _env.Stocks.ListAsync(new StockQuery { BranchId = 999 }));
    }

    private SaveProductRequest ProductRequest(string name, int categoryId) => new()
    {
        Name = name,
        Presentation = "Tabletas",
        CategoryId = categoryId,
        SupplierId = _supplierId,
        Cost = 10m,
        Price = 15m,
    };

    private async Task<ProductDetailDto> CreateProduct(string name, int categoryId, int? minStock = null)
    {
        var request = ProductRequest(name, categoryId);
        request.MinStock = minStock;
        return await _env.Products.CreateAsync(request);
    }

    private async Task Buy(int productId, int quantity)
    {
        var method = await _env.PaymentMethods.CreateAsync(new SavePaymentMethodRequest { Name = "Efectivo" });
        await _env.Purchases.CreateAsync(new SavePurchaseRequest
        {
            PurchaseDate = new DateOnly(2026, 1, 10),
            BranchId = _env.MainBranch.Id,
            SupplierId = _supplierId,
            DocumentNumber = "FAC-1",
            Type = PurchaseType.Cash,
            PaymentMethodId = method.Id,
            Items = [new SavePurchaseItemRequest { ProductId = productId, Quantity = quantity, UnitCost = 8m }],
        });
    }
}
