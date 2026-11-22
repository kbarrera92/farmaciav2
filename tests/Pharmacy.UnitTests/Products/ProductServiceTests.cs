using Pharmacy.Core.Branches;
using Pharmacy.Core.Categories;
using Pharmacy.Core.Common;
using Pharmacy.Core.Products;
using Pharmacy.Core.Suppliers;
using Pharmacy.UnitTests.TestSupport;

namespace Pharmacy.UnitTests.Products;

public sealed class ProductServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private readonly int _categoryId;
    private readonly int _supplierId;

    public ProductServiceTests()
    {
        _categoryId = _env.Categories.CreateAsync(new SaveCategoryRequest { Name = "Analgésicos" }).Result.Id;
        _supplierId = _env.Suppliers.CreateAsync(new SaveSupplierRequest { Name = "Droguería Norte" }).Result.Id;
    }

    public void Dispose() => _env.Dispose();

    [Fact]
    public async Task Create_normalizes_texts_and_stores_defaults()
    {
        var request = Request("  acetaminofén 500 mg ", "tabletas");
        request.Barcode = " 7501234567890 ";
        request.Laboratory = "genfar";
        request.MinStock = 10;
        request.IsControlled = true;

        var product = await _env.Products.CreateAsync(request);

        Assert.Equal("Acetaminofén 500 mg", product.Name);
        Assert.Equal("Tabletas", product.Presentation);
        Assert.Equal("7501234567890", product.Barcode);
        Assert.Equal("Genfar", product.Laboratory);
        Assert.Equal(10, product.MinStock);
        Assert.True(product.IsControlled);
        Assert.False(product.IsBonusEligible);
        Assert.True(product.IsActive);
        Assert.Empty(product.Branches);
    }

    [Fact]
    public async Task Price_and_cost_must_be_positive_and_price_must_exceed_cost()
    {
        await AssertInvalid(Request(price: 10m, cost: 10m), "price");
        await AssertInvalid(Request(price: 5m, cost: 10m), "price");
        await AssertInvalid(Request(price: 10m, cost: 0m), "cost");
        await AssertInvalid(Request(price: 0m, cost: 0m), "price", "cost");
        await AssertInvalid(Request(price: 10.123m, cost: 5m), "price");
        await AssertInvalid(Request(price: 100_000_000m, cost: 5m), "price");
    }

    [Fact]
    public async Task Negative_minimum_stock_is_rejected()
    {
        var request = Request();
        request.MinStock = -1;

        await AssertInvalid(request, "minStock");
    }

    [Fact]
    public async Task Category_and_supplier_must_exist_and_be_active_when_chosen()
    {
        var request = Request();
        request.CategoryId = 999;
        request.SupplierId = 999;
        await AssertInvalid(request, "categoryId", "supplierId");

        await _env.Categories.SetStatusAsync(_categoryId, isActive: false);
        await _env.Suppliers.SetStatusAsync(_supplierId, isActive: false);
        await AssertInvalid(Request(), "categoryId", "supplierId");
    }

    [Fact]
    public async Task Product_keeps_an_inactive_category_and_supplier_it_already_had()
    {
        var product = await _env.Products.CreateAsync(Request());
        await _env.Categories.SetStatusAsync(_categoryId, isActive: false);
        await _env.Suppliers.SetStatusAsync(_supplierId, isActive: false);

        var updated = await _env.Products.UpdateAsync(product.Id, Request(price: 20m));

        Assert.Equal(20m, updated.Price);
        Assert.Equal(_categoryId, updated.Category.Id);
        Assert.Equal(_supplierId, updated.Supplier.Id);
    }

    [Fact]
    public async Task Name_and_presentation_together_must_be_unique_ignoring_case_and_accents()
    {
        await _env.Products.CreateAsync(Request("Acetaminofén 500 mg", "Tabletas"));

        await Assert.ThrowsAsync<ConflictException>(
            () => _env.Products.CreateAsync(Request("ACETAMINOFEN 500 MG", " tabletas ")));

        var otherPresentation = await _env.Products.CreateAsync(Request("Acetaminofén 500 mg", "Jarabe"));
        Assert.Equal("Jarabe", otherPresentation.Presentation);
    }

    [Fact]
    public async Task Barcode_is_unique_when_informed_and_many_products_may_have_none()
    {
        var first = Request("Uno");
        first.Barcode = "123";
        await _env.Products.CreateAsync(first);

        var withoutBarcode = Request("Dos");
        withoutBarcode.Barcode = "   ";
        Assert.Null((await _env.Products.CreateAsync(withoutBarcode)).Barcode);
        Assert.Null((await _env.Products.CreateAsync(Request("Tres"))).Barcode);

        var duplicate = Request("Cuatro");
        duplicate.Barcode = " 123 ";
        await Assert.ThrowsAsync<ConflictException>(() => _env.Products.CreateAsync(duplicate));
    }

    [Fact]
    public async Task Updating_a_product_keeps_its_own_name_and_barcode_but_not_those_of_another()
    {
        var request = Request("Uno");
        request.Barcode = "123";
        var product = await _env.Products.CreateAsync(request);

        request.Cost = 6m;
        var updated = await _env.Products.UpdateAsync(product.Id, request);
        Assert.Equal(6m, updated.Cost);

        var other = Request("Dos");
        other.Barcode = "456";
        var second = await _env.Products.CreateAsync(other);
        other.Barcode = "123";
        await Assert.ThrowsAsync<ConflictException>(() => _env.Products.UpdateAsync(second.Id, other));
    }

    [Fact]
    public async Task Branch_settings_are_stored_and_rows_without_data_are_dropped()
    {
        var south = await CreateBranch("Sur");
        var request = Request(price: 20m, cost: 10m);
        request.MinStock = 5;
        request.Branches =
        [
            new ProductBranchRequest { BranchId = _env.MainBranch.Id, Price = 25m, MinStock = 2, Shelf = " A-3 " },
            new ProductBranchRequest { BranchId = south.Id },
        ];

        var product = await _env.Products.CreateAsync(request);

        var branch = Assert.Single(product.Branches);
        Assert.Equal(_env.MainBranch.Id, branch.BranchId);
        Assert.Equal(25m, branch.Price);
        Assert.Equal(2, branch.MinStock);
        Assert.Equal("A-3", branch.Shelf);
    }

    [Fact]
    public async Task A_branch_may_set_only_the_shelf_and_keep_the_default_price()
    {
        var request = Request();
        request.Branches = [new ProductBranchRequest { BranchId = _env.MainBranch.Id, Shelf = "Vitrina 2" }];

        var product = await _env.Products.CreateAsync(request);

        var branch = Assert.Single(product.Branches);
        Assert.Null(branch.Price);
        Assert.Null(branch.MinStock);
        Assert.Equal("Vitrina 2", branch.Shelf);
    }

    [Fact]
    public async Task Branch_price_must_exceed_the_cost_and_be_positive()
    {
        foreach (var branchPrice in new[] { 10m, 4m, 0m, -1m })
        {
            var request = Request(price: 20m, cost: 10m);
            request.Branches = [new ProductBranchRequest { BranchId = _env.MainBranch.Id, Price = branchPrice }];

            await AssertInvalid(request, "branches");
        }
    }

    [Fact]
    public async Task Branch_rows_must_be_unique_existing_and_active()
    {
        var closed = await CreateBranch("Cerrada");
        await _env.Branches.SetStatusAsync(closed.Id, isActive: false);

        var repeated = Request();
        repeated.Branches =
        [
            new ProductBranchRequest { BranchId = _env.MainBranch.Id, Shelf = "A" },
            new ProductBranchRequest { BranchId = _env.MainBranch.Id, Shelf = "B" },
        ];
        await AssertInvalid(repeated, "branches");

        var unknown = Request();
        unknown.Branches = [new ProductBranchRequest { BranchId = 999, Shelf = "A" }];
        await AssertInvalid(unknown, "branches");

        var inactive = Request();
        inactive.Branches = [new ProductBranchRequest { BranchId = closed.Id, Shelf = "A" }];
        await AssertInvalid(inactive, "branches");
    }

    [Fact]
    public async Task Update_replaces_the_branch_settings()
    {
        var south = await CreateBranch("Sur");
        var request = Request();
        request.Branches =
        [
            new ProductBranchRequest { BranchId = _env.MainBranch.Id, Price = 25m },
            new ProductBranchRequest { BranchId = south.Id, Shelf = "B-1" },
        ];
        var product = await _env.Products.CreateAsync(request);

        request.Branches = [new ProductBranchRequest { BranchId = south.Id, Price = 30m, Shelf = "B-2" }];
        var updated = await _env.Products.UpdateAsync(product.Id, request);

        var branch = Assert.Single(updated.Branches);
        Assert.Equal(south.Id, branch.BranchId);
        Assert.Equal(30m, branch.Price);
        Assert.Equal("B-2", branch.Shelf);
    }

    [Fact]
    public async Task Changing_only_a_branch_setting_marks_the_product_as_modified()
    {
        var request = Request();
        request.Branches = [new ProductBranchRequest { BranchId = _env.MainBranch.Id, Price = 25m }];
        var product = await _env.Products.CreateAsync(request);

        _env.Clock.Advance(TimeSpan.FromHours(1));
        request.Branches = [new ProductBranchRequest { BranchId = _env.MainBranch.Id, Price = 26m }];
        var updated = await _env.Products.UpdateAsync(product.Id, request);

        Assert.True(updated.Audit.UpdatedAtUtc > product.Audit.UpdatedAtUtc);
        Assert.Equal(26m, Assert.Single(updated.Branches).Price);
    }

    [Fact]
    public async Task List_filters_searches_and_pages()
    {
        var other = await _env.Categories.CreateAsync(new SaveCategoryRequest { Name = "Vitaminas" });
        await _env.Products.CreateAsync(Request("Acetaminofén", "Tabletas"));
        var controlled = Request("Tramadol", "Cápsulas");
        controlled.IsControlled = true;
        await _env.Products.CreateAsync(controlled);
        var vitamin = Request("Vitamina C", "Tabletas");
        vitamin.CategoryId = other.Id;
        vitamin.IsBonusEligible = true;
        vitamin.Barcode = "999";
        var created = await _env.Products.CreateAsync(vitamin);
        await _env.Products.SetStatusAsync(created.Id, isActive: false);

        Assert.Equal(3, (await _env.Products.ListAsync(new ProductQuery())).TotalCount);
        Assert.Equal(2, (await _env.Products.ListAsync(new ProductQuery { IsActive = true })).TotalCount);
        Assert.Equal(["Tramadol"], Names(await _env.Products.ListAsync(new ProductQuery { IsControlled = true })));
        Assert.Equal(["Vitamina C"], Names(await _env.Products.ListAsync(new ProductQuery { IsBonusEligible = true })));
        Assert.Equal(["Vitamina C"], Names(await _env.Products.ListAsync(new ProductQuery { CategoryId = other.Id })));
        Assert.Equal(["Vitamina C"], Names(await _env.Products.ListAsync(new ProductQuery { Search = "999" })));

        var firstPage = await _env.Products.ListAsync(new ProductQuery { Page = 1, PageSize = 2 });
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(["Acetaminofén", "Tramadol"], firstPage.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task Unknown_product_returns_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _env.Products.GetAsync(999));
        await Assert.ThrowsAsync<NotFoundException>(() => _env.Products.UpdateAsync(999, Request()));
        await Assert.ThrowsAsync<NotFoundException>(() => _env.Products.SetStatusAsync(999, isActive: false));
    }

    private static IEnumerable<string> Names(PagedResult<ProductListItemDto> page) => page.Items.Select(p => p.Name);

    private SaveProductRequest Request(string name = "Acetaminofén", string presentation = "Tabletas",
        decimal price = 15m, decimal cost = 10m) => new()
    {
        Name = name,
        Presentation = presentation,
        CategoryId = _categoryId,
        SupplierId = _supplierId,
        Price = price,
        Cost = cost,
    };

    private Task<BranchDto> CreateBranch(string name) =>
        _env.Branches.CreateAsync(new SaveBranchRequest { Name = name, Address = "Zona 2" });

    private async Task AssertInvalid(SaveProductRequest request, params string[] fields)
    {
        var error = await Assert.ThrowsAsync<ValidationException>(() => _env.Products.CreateAsync(request));
        foreach (var field in fields)
            Assert.Contains(field, error.Errors.Keys);
    }
}
