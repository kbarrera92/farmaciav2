using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Branches;
using Pharmacy.Core.Categories;
using Pharmacy.Core.Common;
using Pharmacy.Core.PaymentMethods;
using Pharmacy.Core.Products;
using Pharmacy.Core.Purchases;
using Pharmacy.Core.Suppliers;
using Pharmacy.UnitTests.TestSupport;

namespace Pharmacy.UnitTests.Purchases;

public sealed class PurchaseServiceTests : IDisposable
{
    private static readonly DateOnly PurchaseDay = new(2026, 1, 10);

    private readonly TestEnvironment _env = new();
    private readonly int _supplierId;
    private readonly int _paymentMethodId;
    private readonly int _aspirinId;
    private readonly int _ibuprofenId;

    public PurchaseServiceTests()
    {
        var categoryId = _env.Categories.CreateAsync(new SaveCategoryRequest { Name = "Analgésicos" }).Result.Id;
        _supplierId = _env.Suppliers.CreateAsync(new SaveSupplierRequest { Name = "Droguería Norte" }).Result.Id;
        _paymentMethodId = _env.PaymentMethods.CreateAsync(new SavePaymentMethodRequest { Name = "Efectivo" }).Result.Id;
        _aspirinId = CreateProduct("Aspirina", categoryId, cost: 10m, price: 15m).Result;
        _ibuprofenId = CreateProduct("Ibuprofeno", categoryId, cost: 20m, price: 30m).Result;
    }

    public void Dispose() => _env.Dispose();

    [Fact]
    public async Task Cash_purchase_stores_totals_creates_stock_and_has_no_account_payable()
    {
        _env.CurrentUser.UserId = _env.Admin.Id;

        var purchase = await _env.Purchases.CreateAsync(CashRequest(
            Line(_aspirinId, 10, 8.5m), Line(_ibuprofenId, 4, 20m)));

        Assert.Equal(PurchaseType.Cash, purchase.Type);
        Assert.Equal(165m, purchase.Total);
        Assert.Equal(PurchaseDay, purchase.PaymentDate);
        Assert.Equal(_paymentMethodId, purchase.PaymentMethod!.Id);
        Assert.Null(purchase.CreditDays);
        Assert.Null(purchase.AccountPayable);
        Assert.Equal([85m, 80m], purchase.Items.Select(i => i.Subtotal).ToArray());
        Assert.Equal("Administrador", purchase.Audit.CreatedBy);
        Assert.Equal(10, await StockOf(_aspirinId));
        Assert.Equal(4, await StockOf(_ibuprofenId));
    }

    [Fact]
    public async Task Credit_purchase_creates_the_account_payable_with_the_due_date()
    {
        var purchase = await _env.Purchases.CreateAsync(CreditRequest(15, Line(_aspirinId, 10, 8m)));

        Assert.Equal(PurchaseType.Credit, purchase.Type);
        Assert.Null(purchase.PaymentMethod);
        Assert.Null(purchase.PaymentDate);
        Assert.Equal(15, purchase.CreditDays);
        var payable = purchase.AccountPayable!;
        Assert.Equal(PurchaseDay, payable.StartDate);
        Assert.Equal(PurchaseDay.AddDays(15), payable.DueDate);
        Assert.Equal(80m, payable.Total);
        Assert.Equal(80m, payable.Balance);
        Assert.Equal(AccountPayableStatus.Pending, payable.Status);
        Assert.False(payable.HasPayments);
    }

    [Fact]
    public async Task Purchases_add_to_the_existing_stock()
    {
        await _env.Purchases.CreateAsync(CashRequest("A-1", Line(_aspirinId, 10, 8m)));
        await _env.Purchases.CreateAsync(CashRequest("A-2", Line(_aspirinId, 5, 9m)));

        Assert.Equal(15, await StockOf(_aspirinId));
    }

    [Fact]
    public async Task Purchase_cost_is_not_written_to_the_product_and_warns_when_not_below_the_sale_price()
    {
        var purchase = await _env.Purchases.CreateAsync(CashRequest(Line(_aspirinId, 1, 15m), Line(_ibuprofenId, 1, 25m)));

        var aspirin = purchase.Items.Single(i => i.ProductId == _aspirinId);
        var ibuprofen = purchase.Items.Single(i => i.ProductId == _ibuprofenId);
        Assert.True(aspirin.CostAtOrAboveSalePrice);
        Assert.Equal(15m, aspirin.SalePrice);
        Assert.False(ibuprofen.CostAtOrAboveSalePrice);
        Assert.Equal(10m, (await _env.Products.GetAsync(_aspirinId)).Cost);
    }

    [Fact]
    public async Task Document_is_unique_per_supplier_without_case_and_released_by_cancelling()
    {
        var first = await _env.Purchases.CreateAsync(CashRequest("fac-100", Line(_aspirinId, 1, 8m)));

        await Assert.ThrowsAsync<ConflictException>(() =>
            _env.Purchases.CreateAsync(CashRequest(" FAC-100 ", Line(_aspirinId, 1, 8m))));

        var otherSupplier = await _env.Suppliers.CreateAsync(new SaveSupplierRequest { Name = "Distribuidora Sur" });
        var request = CashRequest("FAC-100", Line(_aspirinId, 1, 8m));
        request.SupplierId = otherSupplier.Id;
        await _env.Purchases.CreateAsync(request);

        await _env.Purchases.CancelAsync(first.Id, "Error de captura");
        var again = await _env.Purchases.CreateAsync(CashRequest("FAC-100", Line(_aspirinId, 1, 8m)));
        Assert.NotEqual(first.Id, again.Id);
    }

    [Fact]
    public async Task Invalid_purchases_are_rejected_with_the_field_errors()
    {
        await AssertInvalid(Mutate(r => r.PurchaseDate = new DateOnly(2026, 1, 16)), "purchaseDate");
        await AssertInvalid(Mutate(r => r.DocumentNumber = "   "), "documentNumber");
        await AssertInvalid(Mutate(r => r.Items = []), "items");
        await AssertInvalid(Mutate(r => r.Items = [Line(_aspirinId, 1, 8m), Line(_aspirinId, 2, 8m)]), "items");
        await AssertInvalid(Mutate(r => r.Items = [Line(_aspirinId, 0, 8m)]), "items");
        await AssertInvalid(Mutate(r => r.Items = [Line(_aspirinId, 1, 0m)]), "items");
        await AssertInvalid(Mutate(r => r.Items = [Line(_aspirinId, 1, 8.123m)]), "items");
        await AssertInvalid(Mutate(r => r.Items = [Line(999, 1, 8m)]), "items");
        await AssertInvalid(Mutate(r => r.Items = [Line(_aspirinId, 1_000_000, 99_999m)]), "items");
        await AssertInvalid(Mutate(r => r.SupplierId = 999), "supplierId");
        await AssertInvalid(Mutate(r => r.BranchId = 999), "branchId");
    }

    [Fact]
    public async Task Payment_data_must_match_the_purchase_type()
    {
        await AssertInvalid(Mutate(r => r.PaymentMethodId = null), "paymentMethodId");
        await AssertInvalid(Mutate(r => r.PaymentMethodId = 999), "paymentMethodId");
        await AssertInvalid(Mutate(r => r.CreditDays = 15), "creditDays");

        var credit = CreditRequest(15, Line(_aspirinId, 1, 8m));
        credit.CreditDays = null;
        await AssertInvalid(credit, "creditDays");
        credit.CreditDays = 0;
        await AssertInvalid(credit, "creditDays");
        credit.CreditDays = 366;
        await AssertInvalid(credit, "creditDays");
        credit.CreditDays = 30;
        credit.PaymentMethodId = _paymentMethodId;
        await AssertInvalid(credit, "paymentMethodId");
    }

    [Fact]
    public async Task Inactive_supplier_branch_product_and_payment_method_are_not_offered()
    {
        await _env.Suppliers.SetStatusAsync(_supplierId, false);
        await AssertInvalid(Mutate(_ => { }), "supplierId");
        await _env.Suppliers.SetStatusAsync(_supplierId, true);

        var closed = await _env.Branches.CreateAsync(new SaveBranchRequest { Name = "Cerrada", Address = "Zona 3" });
        await _env.Branches.SetStatusAsync(closed.Id, false);
        await AssertInvalid(Mutate(r => r.BranchId = closed.Id), "branchId");

        await _env.Products.SetStatusAsync(_aspirinId, false);
        await AssertInvalid(Mutate(_ => { }), "items");
        await _env.Products.SetStatusAsync(_aspirinId, true);

        await _env.PaymentMethods.SetStatusAsync(_paymentMethodId, false);
        await AssertInvalid(Mutate(_ => { }), "paymentMethodId");
    }

    [Fact]
    public async Task Update_adjusts_the_stock_by_the_difference_including_added_and_removed_lines()
    {
        var purchase = await _env.Purchases.CreateAsync(CashRequest(Line(_aspirinId, 10, 8m), Line(_ibuprofenId, 4, 20m)));

        var request = CashRequest(Line(_aspirinId, 6, 9m));
        var updated = await _env.Purchases.UpdateAsync(purchase.Id, request);

        Assert.Equal(54m, updated.Total);
        Assert.Single(updated.Items);
        Assert.Equal(6, await StockOf(_aspirinId));
        Assert.Equal(0, await StockOf(_ibuprofenId));

        var more = CashRequest(Line(_aspirinId, 6, 9m), Line(_ibuprofenId, 3, 20m));
        await _env.Purchases.UpdateAsync(purchase.Id, more);
        Assert.Equal(3, await StockOf(_ibuprofenId));
    }

    [Fact]
    public async Task Update_can_switch_between_cash_and_credit()
    {
        var purchase = await _env.Purchases.CreateAsync(CreditRequest(30, Line(_aspirinId, 10, 8m)));
        Assert.NotNull(purchase.AccountPayable);

        var toCash = await _env.Purchases.UpdateAsync(purchase.Id, CashRequest(Line(_aspirinId, 10, 8m)));
        Assert.Null(toCash.AccountPayable);
        Assert.Equal(PurchaseDay, toCash.PaymentDate);
        Assert.Equal(0, await _env.Db.AccountsPayable.CountAsync());

        var toCredit = await _env.Purchases.UpdateAsync(purchase.Id, CreditRequest(20, Line(_aspirinId, 5, 8m)));
        Assert.Equal(40m, toCredit.AccountPayable!.Total);
        Assert.Equal(PurchaseDay.AddDays(20), toCredit.AccountPayable.DueDate);
        Assert.Null(toCredit.PaymentMethod);
        Assert.Null(toCredit.PaymentDate);
        Assert.Equal(5, await StockOf(_aspirinId));
    }

    [Fact]
    public async Task Update_cannot_change_the_branch()
    {
        var purchase = await _env.Purchases.CreateAsync(CashRequest(Line(_aspirinId, 1, 8m)));
        var other = await _env.Branches.CreateAsync(new SaveBranchRequest { Name = "Norte", Address = "Zona 2" });

        var request = CashRequest(Line(_aspirinId, 1, 8m));
        request.BranchId = other.Id;

        await AssertInvalid(request, "branchId", purchaseId: purchase.Id);
    }

    [Fact]
    public async Task Cancel_requires_a_reason_marks_the_purchase_and_subtracts_the_stock()
    {
        _env.CurrentUser.UserId = _env.Admin.Id;
        var purchase = await _env.Purchases.CreateAsync(CreditRequest(15, Line(_aspirinId, 10, 8m)));

        await Assert.ThrowsAsync<ValidationException>(() => _env.Purchases.CancelAsync(purchase.Id, "  "));

        var cancelled = await _env.Purchases.CancelAsync(purchase.Id, " Factura duplicada ");

        Assert.True(cancelled.IsCancelled);
        Assert.Equal("Factura duplicada", cancelled.CancellationReason);
        Assert.Equal("Administrador", cancelled.CancelledBy);
        Assert.NotNull(cancelled.CancelledAtUtc);
        Assert.Null(cancelled.AccountPayable);
        Assert.Equal(80m, cancelled.Total);
        Assert.Equal(0, await StockOf(_aspirinId));
        Assert.Equal(0, await _env.Db.AccountsPayable.CountAsync());
    }

    [Fact]
    public async Task A_cancelled_purchase_cannot_be_edited_or_cancelled_again()
    {
        var purchase = await _env.Purchases.CreateAsync(CashRequest(Line(_aspirinId, 1, 8m)));
        await _env.Purchases.CancelAsync(purchase.Id, "Error");

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _env.Purchases.UpdateAsync(purchase.Id, CashRequest(Line(_aspirinId, 2, 8m))));
        await Assert.ThrowsAsync<BusinessRuleException>(() => _env.Purchases.CancelAsync(purchase.Id, "Otra vez"));
        Assert.Equal(0, await StockOf(_aspirinId));
    }

    [Fact]
    public async Task A_credit_purchase_with_payments_cannot_be_edited_or_cancelled()
    {
        var purchase = await _env.Purchases.CreateAsync(CreditRequest(15, Line(_aspirinId, 10, 8m)));
        await _env.Db.AccountsPayable.Where(a => a.PurchaseId == purchase.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.Balance, 30m));
        _env.Db.ChangeTracker.Clear();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _env.Purchases.UpdateAsync(purchase.Id, CreditRequest(15, Line(_aspirinId, 1, 8m))));
        await Assert.ThrowsAsync<BusinessRuleException>(() => _env.Purchases.CancelAsync(purchase.Id, "Error"));
        Assert.Equal(10, await StockOf(_aspirinId));
    }

    [Fact]
    public async Task Unknown_purchase_is_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _env.Purchases.GetAsync(999));
        await Assert.ThrowsAsync<NotFoundException>(() => _env.Purchases.CancelAsync(999, "Error"));
    }

    [Fact]
    public async Task List_filters_and_orders_by_date_descending()
    {
        var old = CashRequest("F-1", Line(_aspirinId, 1, 8m));
        old.PurchaseDate = new DateOnly(2026, 1, 2);
        var oldPurchase = await _env.Purchases.CreateAsync(old);
        var recent = await _env.Purchases.CreateAsync(CreditRequest("F-2", 15, Line(_aspirinId, 1, 8m)));
        await _env.Purchases.CancelAsync(oldPurchase.Id, "Error");

        var all = await _env.Purchases.ListAsync(new PurchaseQuery());
        Assert.Equal([recent.Id, oldPurchase.Id], all.Items.Select(i => i.Id).ToArray());

        Assert.Equal([recent.Id],
            (await _env.Purchases.ListAsync(new PurchaseQuery { IsCancelled = false })).Items.Select(i => i.Id).ToArray());
        Assert.Equal([oldPurchase.Id],
            (await _env.Purchases.ListAsync(new PurchaseQuery { To = new DateOnly(2026, 1, 5) })).Items.Select(i => i.Id).ToArray());
        Assert.Equal([recent.Id],
            (await _env.Purchases.ListAsync(new PurchaseQuery { Type = PurchaseType.Credit })).Items.Select(i => i.Id).ToArray());
        Assert.Equal([recent.Id],
            (await _env.Purchases.ListAsync(new PurchaseQuery { Search = "F-2" })).Items.Select(i => i.Id).ToArray());
        Assert.Empty((await _env.Purchases.ListAsync(new PurchaseQuery { SupplierId = 999 })).Items);
    }

    [Fact]
    public async Task Purchasable_products_show_the_branch_stock_and_sale_price()
    {
        await _env.Purchases.CreateAsync(CashRequest(Line(_aspirinId, 7, 8m)));
        await _env.Products.SetStatusAsync(_ibuprofenId, false);

        var products = await _env.Purchases.ListProductsAsync(new PurchaseProductQuery { BranchId = _env.MainBranch.Id });

        var aspirin = Assert.Single(products.Items);
        Assert.Equal(_aspirinId, aspirin.Id);
        Assert.Equal(7, aspirin.Stock);
        Assert.Equal(10m, aspirin.Cost);
        Assert.Equal(15m, aspirin.SalePrice);

        var otherBranch = await _env.Branches.CreateAsync(new SaveBranchRequest { Name = "Norte", Address = "Zona 2" });
        var elsewhere = await _env.Purchases.ListProductsAsync(new PurchaseProductQuery { BranchId = otherBranch.Id });
        Assert.Equal(0, Assert.Single(elsewhere.Items).Stock);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _env.Purchases.ListProductsAsync(new PurchaseProductQuery { BranchId = 999 }));
    }

    private async Task<int> CreateProduct(string name, int categoryId, decimal cost, decimal price) =>
        (await _env.Products.CreateAsync(new SaveProductRequest
        {
            Name = name,
            Presentation = "Tabletas",
            CategoryId = categoryId,
            SupplierId = _supplierId,
            Cost = cost,
            Price = price,
        })).Id;

    private static SavePurchaseItemRequest Line(int productId, int quantity, decimal cost) =>
        new() { ProductId = productId, Quantity = quantity, UnitCost = cost };

    private SavePurchaseRequest CashRequest(params SavePurchaseItemRequest[] items) => CashRequest("FAC-001", items);

    private SavePurchaseRequest CashRequest(string document, params SavePurchaseItemRequest[] items) => new()
    {
        PurchaseDate = PurchaseDay,
        BranchId = _env.MainBranch.Id,
        SupplierId = _supplierId,
        DocumentNumber = document,
        Type = PurchaseType.Cash,
        PaymentMethodId = _paymentMethodId,
        Items = [.. items],
    };

    private SavePurchaseRequest CreditRequest(int days, params SavePurchaseItemRequest[] items) =>
        CreditRequest("FAC-001", days, items);

    private SavePurchaseRequest CreditRequest(string document, int days, params SavePurchaseItemRequest[] items) => new()
    {
        PurchaseDate = PurchaseDay,
        BranchId = _env.MainBranch.Id,
        SupplierId = _supplierId,
        DocumentNumber = document,
        Type = PurchaseType.Credit,
        CreditDays = days,
        Items = [.. items],
    };

    /// <summary>A valid cash request with the given change applied.</summary>
    private SavePurchaseRequest Mutate(Action<SavePurchaseRequest> change)
    {
        var request = CashRequest(Line(_aspirinId, 1, 8m));
        change(request);
        return request;
    }

    private async Task<int> StockOf(int productId) =>
        await _env.Db.Stocks.AsNoTracking()
            .Where(s => s.BranchId == _env.MainBranch.Id && s.ProductId == productId)
            .Select(s => s.Quantity)
            .SingleOrDefaultAsync();

    private async Task AssertInvalid(SavePurchaseRequest request, string field, int? purchaseId = null)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => purchaseId is { } id
            ? _env.Purchases.UpdateAsync(id, request)
            : _env.Purchases.CreateAsync(request));
        Assert.Contains(field, exception.Errors.Keys);
    }
}
