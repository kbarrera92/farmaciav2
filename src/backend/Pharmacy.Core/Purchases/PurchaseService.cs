using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;
using Pharmacy.Core.Stocks;

namespace Pharmacy.Core.Purchases;

public sealed class PurchaseService(
    IAppDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock,
    IOptions<BusinessOptions> business)
{
    private const int MaxQuantity = 1_000_000;
    private const int MaxCreditDays = 365;

    public async Task<PagedResult<PurchaseListItemDto>> ListAsync(PurchaseQuery query, CancellationToken ct = default)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        var purchases = db.Purchases.AsNoTracking();

        if (TextNormalizer.TrimToNull(query.Search) is { } search)
            purchases = purchases.Where(p => p.DocumentNumber.Contains(search));
        if (query.BranchId is { } branchId)
            purchases = purchases.Where(p => p.BranchId == branchId);
        if (query.SupplierId is { } supplierId)
            purchases = purchases.Where(p => p.SupplierId == supplierId);
        if (query.Type is { } type)
            purchases = purchases.Where(p => p.Type == type);
        if (query.IsCancelled is { } isCancelled)
            purchases = purchases.Where(p => p.IsCancelled == isCancelled);
        if (query.From is { } from)
            purchases = purchases.Where(p => p.PurchaseDate >= from);
        if (query.To is { } to)
            purchases = purchases.Where(p => p.PurchaseDate <= to);

        var total = await purchases.CountAsync(ct);
        var items = await purchases
            .OrderByDescending(p => p.PurchaseDate).ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PurchaseListItemDto(
                p.Id, p.PurchaseDate, p.DocumentNumber,
                new NamedReference(p.SupplierId, p.Supplier!.Name),
                new NamedReference(p.BranchId, p.Branch!.Name),
                p.Type,
                p.PaymentMethod != null ? p.PaymentMethod.Name : null,
                p.PaymentDate, p.Total, p.IsCancelled,
                p.CreatedBy != null ? p.CreatedBy.FullName : null))
            .ToListAsync(ct);

        return new PagedResult<PurchaseListItemDto>(items, page, pageSize, total);
    }

    public async Task<PurchaseDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var purchase = await db.Purchases.AsNoTracking()
            .Include(p => p.Branch)
            .Include(p => p.Supplier)
            .Include(p => p.PaymentMethod)
            .Include(p => p.CreatedBy)
            .Include(p => p.UpdatedBy)
            .Include(p => p.CancelledBy)
            .Include(p => p.AccountPayable)
            .Include(p => p.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw NotFound();

        var productIds = purchase.Items.Select(i => i.ProductId).ToList();
        var branchId = purchase.BranchId;
        var salePrices = await db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                Price = p.BranchSettings.Where(s => s.BranchId == branchId).Select(s => s.Price).FirstOrDefault() ?? p.Price,
            })
            .ToDictionaryAsync(p => p.Id, p => p.Price, ct);

        var items = purchase.Items
            .OrderBy(i => i.Product!.Name).ThenBy(i => i.Product!.Presentation).ThenBy(i => i.Id)
            .Select(i =>
            {
                var salePrice = salePrices[i.ProductId];
                return new PurchaseItemDto(
                    i.ProductId, i.Product!.Name, i.Product.Presentation, i.Quantity, i.UnitCost, i.Subtotal,
                    salePrice, i.UnitCost >= salePrice);
            })
            .ToList();

        var payable = purchase.AccountPayable is { } a
            ? new AccountPayableDto(a.Id, a.StartDate, a.DueDate, a.Total, a.Balance, a.Status, a.HasPayments)
            : null;

        return new PurchaseDetailDto(
            purchase.Id, purchase.PurchaseDate, purchase.DocumentNumber,
            new NamedReference(purchase.SupplierId, purchase.Supplier!.Name),
            new NamedReference(purchase.BranchId, purchase.Branch!.Name),
            purchase.Type,
            purchase.PaymentMethod is { } method ? new NamedReference(method.Id, method.Name) : null,
            purchase.PaymentDate, purchase.CreditDays, purchase.Total,
            purchase.IsCancelled, purchase.CancelledAtUtc, purchase.CancelledBy?.FullName, purchase.CancellationReason,
            items, payable,
            new AuditInfo(purchase.CreatedAtUtc, purchase.CreatedBy?.FullName, purchase.UpdatedAtUtc, purchase.UpdatedBy?.FullName));
    }

    /// <summary>Active products of any supplier with the existence and sale price of the branch.</summary>
    public async Task<PagedResult<PurchaseProductDto>> ListProductsAsync(PurchaseProductQuery query, CancellationToken ct = default)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        var branchId = query.BranchId!.Value;
        if (!await db.Branches.AnyAsync(b => b.Id == branchId, ct))
            throw new NotFoundException("La sucursal no existe.");

        var products = db.Products.AsNoTracking().Where(p => p.IsActive);
        if (TextNormalizer.TrimToNull(query.Search) is { } search)
        {
            products = products.Where(p =>
                p.Name.Contains(search)
                || p.Presentation.Contains(search)
                || (p.Barcode != null && p.Barcode.Contains(search))
                || (p.Laboratory != null && p.Laboratory.Contains(search)));
        }

        var total = await products.CountAsync(ct);
        var items = await products
            .OrderBy(p => p.Name).ThenBy(p => p.Presentation).ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PurchaseProductDto(
                p.Id, p.Name, p.Presentation, p.Laboratory,
                new NamedReference(p.CategoryId, p.Category!.Name),
                new NamedReference(p.SupplierId, p.Supplier!.Name),
                db.Stocks.Where(s => s.BranchId == branchId && s.ProductId == p.Id).Select(s => s.Quantity).FirstOrDefault(),
                p.Cost,
                p.BranchSettings.Where(s => s.BranchId == branchId).Select(s => s.Price).FirstOrDefault() ?? p.Price))
            .ToListAsync(ct);

        return new PagedResult<PurchaseProductDto>(items, page, pageSize, total);
    }

    public async Task<PurchaseDetailDto> CreateAsync(SavePurchaseRequest request, CancellationToken ct = default)
    {
        var purchase = new Purchase();
        await ApplyAsync(purchase, request, ct);
        SyncAccountPayable(purchase);

        var deltas = purchase.Items.ToDictionary(i => i.ProductId, i => i.Quantity);
        db.Purchases.Add(purchase);
        await SaveWithStockAsync(purchase.BranchId, deltas, ct);

        return await GetAsync(purchase.Id, ct);
    }

    public async Task<PurchaseDetailDto> UpdateAsync(int id, SavePurchaseRequest request, CancellationToken ct = default)
    {
        var purchase = await LoadForChangeAsync(id, "editar", ct);
        var before = purchase.Items.ToDictionary(i => i.ProductId, i => i.Quantity);

        await ApplyAsync(purchase, request, ct);
        SyncAccountPayable(purchase);

        var after = purchase.Items.ToDictionary(i => i.ProductId, i => i.Quantity);
        var deltas = before.Keys.Union(after.Keys)
            .ToDictionary(p => p, p => after.GetValueOrDefault(p) - before.GetValueOrDefault(p));
        await SaveWithStockAsync(purchase.BranchId, deltas, ct);

        return await GetAsync(id, ct);
    }

    public async Task<PurchaseDetailDto> CancelAsync(int id, string reason, CancellationToken ct = default)
    {
        var cleanReason = TextNormalizer.TrimToNull(reason)
            ?? throw new ValidationException("reason", "El motivo de la anulación es obligatorio.");
        var purchase = await LoadForChangeAsync(id, "anular", ct);

        var deltas = purchase.Items.ToDictionary(i => i.ProductId, i => -i.Quantity);
        purchase.IsCancelled = true;
        purchase.CancelledAtUtc = clock.GetUtcNow().UtcDateTime;
        purchase.CancelledByUserId = currentUser.UserId;
        purchase.CancellationReason = cleanReason;
        if (purchase.AccountPayable is { } payable)
        {
            db.AccountsPayable.Remove(payable);
            purchase.AccountPayable = null;
        }

        await SaveWithStockAsync(purchase.BranchId, deltas, ct);

        return await GetAsync(id, ct);
    }

    /// <summary>Loads a purchase that is going to be edited or cancelled, checking that it still can be.</summary>
    private async Task<Purchase> LoadForChangeAsync(int id, string action, CancellationToken ct)
    {
        var purchase = await db.Purchases
            .Include(p => p.Items)
            .Include(p => p.AccountPayable)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw NotFound();

        if (purchase.IsCancelled)
            throw new BusinessRuleException($"La compra está anulada y no se puede {action}.");
        if (purchase.AccountPayable is { HasPayments: true })
            throw new BusinessRuleException($"La cuenta por pagar de la compra ya tiene abonos y no se puede {action}.");

        return purchase;
    }

    private async Task ApplyAsync(Purchase purchase, SavePurchaseRequest request, CancellationToken ct)
    {
        var isNew = purchase.Id == 0;
        var errors = new ValidationErrors();

        var date = request.PurchaseDate!.Value;
        if (date > Today())
            errors.Add("purchaseDate", "La fecha de la compra no puede ser futura.");

        var document = TextNormalizer.TrimToNull(request.DocumentNumber);
        if (document is null)
            errors.Add("documentNumber", "El número de documento es obligatorio.");

        await ValidateBranchAsync(purchase, request.BranchId!.Value, isNew, errors, ct);
        await ValidateSupplierAsync(purchase, request.SupplierId!.Value, isNew, errors, ct);
        await ValidatePaymentAsync(purchase, request, isNew, errors, ct);
        var lines = await ValidateItemsAsync(purchase, request, errors, ct);
        errors.ThrowIfAny();

        var supplierId = request.SupplierId!.Value;
        if (await db.Purchases.AnyAsync(
                p => !p.IsCancelled && p.SupplierId == supplierId && p.DocumentNumber == document
                    && (isNew || p.Id != purchase.Id), ct))
        {
            throw new ConflictException($"Ya existe una compra del proveedor con el documento '{document}'.");
        }

        var isCash = request.Type == PurchaseType.Cash;
        purchase.PurchaseDate = date;
        purchase.BranchId = request.BranchId!.Value;
        purchase.SupplierId = supplierId;
        purchase.DocumentNumber = document!;
        purchase.Type = request.Type!.Value;
        purchase.PaymentMethodId = isCash ? request.PaymentMethodId : null;
        purchase.PaymentDate = isCash ? date : null;
        purchase.CreditDays = isCash ? null : request.CreditDays;

        ApplyItems(purchase, lines);
        purchase.Total = purchase.Items.Sum(i => i.Subtotal);
    }

    private async Task ValidateBranchAsync(Purchase purchase, int branchId, bool isNew, ValidationErrors errors, CancellationToken ct)
    {
        if (!isNew && purchase.BranchId != branchId)
        {
            errors.Add("branchId", "La sucursal de una compra registrada no se puede cambiar.");
            return;
        }

        var branch = await db.Branches.AsNoTracking()
            .Where(b => b.Id == branchId).Select(b => new { b.IsActive }).FirstOrDefaultAsync(ct);
        if (branch is null)
            errors.Add("branchId", "La sucursal no existe.");
        else if (!branch.IsActive && isNew)
            errors.Add("branchId", "La sucursal está inactiva.");
    }

    private async Task ValidateSupplierAsync(Purchase purchase, int supplierId, bool isNew, ValidationErrors errors, CancellationToken ct)
    {
        var supplier = await db.Suppliers.AsNoTracking()
            .Where(s => s.Id == supplierId).Select(s => new { s.IsActive }).FirstOrDefaultAsync(ct);
        if (supplier is null)
            errors.Add("supplierId", "El proveedor no existe.");
        else if (!supplier.IsActive && (isNew || purchase.SupplierId != supplierId))
            errors.Add("supplierId", "El proveedor está inactivo.");
    }

    /// <summary>Cash needs a payment method; credit needs the credit days and has no payment method.</summary>
    private async Task ValidatePaymentAsync(
        Purchase purchase, SavePurchaseRequest request, bool isNew, ValidationErrors errors, CancellationToken ct)
    {
        switch (request.Type)
        {
            case PurchaseType.Cash:
                if (request.CreditDays is not null)
                    errors.Add("creditDays", "Los días de crédito solo aplican a compras al crédito.");
                if (request.PaymentMethodId is not { } methodId)
                {
                    errors.Add("paymentMethodId", "La forma de pago es obligatoria en compras de contado.");
                    break;
                }

                var method = await db.PaymentMethods.AsNoTracking()
                    .Where(m => m.Id == methodId).Select(m => new { m.IsActive }).FirstOrDefaultAsync(ct);
                if (method is null)
                    errors.Add("paymentMethodId", "La forma de pago no existe.");
                else if (!method.IsActive && (isNew || purchase.PaymentMethodId != methodId))
                    errors.Add("paymentMethodId", "La forma de pago está inactiva.");
                break;

            case PurchaseType.Credit:
                if (request.PaymentMethodId is not null)
                    errors.Add("paymentMethodId", "La forma de pago solo aplica a compras de contado.");
                if (request.CreditDays is not { } days)
                    errors.Add("creditDays", "Los días de crédito son obligatorios en compras al crédito.");
                else if (days is < 1 or > MaxCreditDays)
                    errors.Add("creditDays", $"Los días de crédito deben estar entre 1 y {MaxCreditDays}.");
                break;

            default:
                errors.Add("type", "El tipo de compra no es válido.");
                break;
        }
    }

    private async Task<List<PurchaseLine>> ValidateItemsAsync(
        Purchase purchase, SavePurchaseRequest request, ValidationErrors errors, CancellationToken ct)
    {
        var lines = new List<PurchaseLine>();
        if (request.Items.Count == 0)
        {
            errors.Add("items", "La compra debe tener al menos un producto.");
            return lines;
        }

        var ids = request.Items.Select(i => i.ProductId!.Value).Distinct().ToList();
        if (ids.Count != request.Items.Count)
            errors.Add("items", "Hay productos repetidos.");

        var products = await db.Products.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.Presentation, p.IsActive })
            .ToDictionaryAsync(p => p.Id, ct);
        var alreadyInPurchase = purchase.Items.Select(i => i.ProductId).ToHashSet();

        foreach (var item in request.Items.DistinctBy(i => i.ProductId))
        {
            var productId = item.ProductId!.Value;
            if (!products.TryGetValue(productId, out var product))
            {
                errors.Add("items", $"El producto {productId} no existe.");
                continue;
            }

            var label = $"{product.Name} ({product.Presentation})";
            if (!product.IsActive && !alreadyInPurchase.Contains(productId))
                errors.Add("items", $"{label}: el producto está inactivo.");

            var quantity = item.Quantity!.Value;
            var cost = item.UnitCost!.Value;
            var before = errors.Count;
            if (quantity is < 1 or > MaxQuantity)
                errors.Add("items", $"{label}: la cantidad debe estar entre 1 y {MaxQuantity:N0}.");
            MoneyRules.Validate(errors, "items", $"{label}: el costo", cost);
            if (errors.Count != before)
                continue;

            if (quantity * cost > MoneyRules.MaxMoney)
            {
                errors.Add("items", $"{label}: el subtotal no puede superar {MoneyRules.MaxMoney:N2}.");
                continue;
            }

            lines.Add(new PurchaseLine(productId, quantity, cost));
        }

        if (lines.Sum(l => l.Quantity * l.UnitCost) > MoneyRules.MaxMoney)
            errors.Add("items", $"El total de la compra no puede superar {MoneyRules.MaxMoney:N2}.");

        return lines;
    }

    /// <summary>Makes the purchase lines match the requested ones (add, update, remove).</summary>
    private static void ApplyItems(Purchase purchase, List<PurchaseLine> lines)
    {
        var requestedIds = lines.Select(l => l.ProductId).ToHashSet();
        purchase.Items.RemoveAll(i => !requestedIds.Contains(i.ProductId));

        foreach (var line in lines)
        {
            var item = purchase.Items.FirstOrDefault(i => i.ProductId == line.ProductId);
            if (item is null)
            {
                item = new PurchaseItem { ProductId = line.ProductId };
                purchase.Items.Add(item);
            }

            item.Quantity = line.Quantity;
            item.UnitCost = line.UnitCost;
            item.Subtotal = line.Quantity * line.UnitCost;
        }
    }

    /// <summary>A credit purchase has an account payable; a cash one does not.</summary>
    private static void SyncAccountPayable(Purchase purchase)
    {
        if (purchase.Type == PurchaseType.Credit)
        {
            var payable = purchase.AccountPayable ?? (purchase.AccountPayable = new AccountPayable());
            payable.SupplierId = purchase.SupplierId;
            payable.StartDate = purchase.PurchaseDate;
            payable.DueDate = purchase.PurchaseDate.AddDays(purchase.CreditDays!.Value);
            payable.Total = purchase.Total;
            payable.Balance = purchase.Total;
            payable.Status = AccountPayableStatus.Pending;
        }
        else if (purchase.AccountPayable is not null)
        {
            // Detaching a required dependent deletes it (cascade of orphans).
            purchase.AccountPayable = null;
        }
    }

    /// <summary>
    /// Saves the purchase and applies the stock changes atomically: stock never changes without the
    /// purchase and vice versa. Wrapped in the execution strategy because SQL Server retries failures.
    /// </summary>
    private async Task SaveWithStockAsync(int branchId, Dictionary<int, int> deltas, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await AdjustStockAsync(branchId, deltas, ct);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        });
    }

    /// <summary>
    /// Adds the delta to the existence of each product. The update runs in the database (no lost
    /// updates under concurrency); the row is created with the first movement.
    /// </summary>
    private async Task AdjustStockAsync(int branchId, Dictionary<int, int> deltas, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var (productId, delta) in deltas.Where(d => d.Value != 0))
        {
            var updated = await db.Stocks
                .Where(s => s.BranchId == branchId && s.ProductId == productId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Quantity, x => x.Quantity + delta)
                    .SetProperty(x => x.UpdatedAtUtc, now), ct);

            // Local check: a retried attempt must not add the same row twice.
            if (updated == 0 && !db.Stocks.Local.Any(s => s.BranchId == branchId && s.ProductId == productId))
                db.Stocks.Add(new Stock { BranchId = branchId, ProductId = productId, Quantity = delta, UpdatedAtUtc = now });
        }
    }

    private DateOnly Today() =>
        DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime.AddHours(business.Value.UtcOffsetHours));

    private static NotFoundException NotFound() => new("La compra no existe.");

    private sealed record PurchaseLine(int ProductId, int Quantity, decimal UnitCost);
}
