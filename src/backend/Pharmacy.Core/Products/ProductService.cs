using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.Products;

public sealed class ProductService(IAppDbContext db)
{
    /// <summary>Largest value that fits the decimal(10,2) price and cost columns.</summary>
    private const decimal MaxMoney = 99_999_999.99m;

    public async Task<PagedResult<ProductListItemDto>> ListAsync(ProductQuery query, CancellationToken ct = default)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        var products = db.Products.AsNoTracking();

        if (TextNormalizer.TrimToNull(query.Search) is { } search)
        {
            products = products.Where(p =>
                p.Name.Contains(search)
                || p.Presentation.Contains(search)
                || (p.Barcode != null && p.Barcode.Contains(search))
                || (p.ActiveIngredient != null && p.ActiveIngredient.Contains(search))
                || (p.Laboratory != null && p.Laboratory.Contains(search)));
        }

        if (query.CategoryId is { } categoryId)
            products = products.Where(p => p.CategoryId == categoryId);
        if (query.SupplierId is { } supplierId)
            products = products.Where(p => p.SupplierId == supplierId);
        if (query.IsActive is { } isActive)
            products = products.Where(p => p.IsActive == isActive);
        if (query.IsControlled is { } isControlled)
            products = products.Where(p => p.IsControlled == isControlled);
        if (query.IsBonusEligible is { } isBonusEligible)
            products = products.Where(p => p.IsBonusEligible == isBonusEligible);

        var total = await products.CountAsync(ct);
        var items = await products
            .OrderBy(p => p.Name).ThenBy(p => p.Presentation).ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListItemDto(
                p.Id, p.Name, p.Presentation, p.Barcode,
                new NamedReference(p.CategoryId, p.Category!.Name),
                new NamedReference(p.SupplierId, p.Supplier!.Name),
                p.Price, p.Cost, p.IsControlled, p.IsBonusEligible, p.IsActive))
            .ToListAsync(ct);

        return new PagedResult<ProductListItemDto>(items, page, pageSize, total);
    }

    public async Task<ProductDetailDto> GetAsync(int id, CancellationToken ct = default) =>
        await db.Products.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDetailDto(
                p.Id, p.Name, p.Presentation, p.ActiveIngredient, p.TherapeuticAction, p.Indications,
                p.Contraindications, p.Notes, p.Measure, p.Laboratory, p.Barcode,
                new NamedReference(p.CategoryId, p.Category!.Name),
                new NamedReference(p.SupplierId, p.Supplier!.Name),
                p.Price, p.Cost, p.MinStock, p.IsControlled, p.IsBonusEligible, p.IsActive,
                p.BranchSettings
                    .OrderBy(s => s.Branch!.Name)
                    .Select(s => new ProductBranchDto(s.BranchId, s.Branch!.Name, s.Price, s.MinStock, s.Shelf))
                    .ToList(),
                new AuditInfo(p.CreatedAtUtc, p.CreatedBy!.FullName, p.UpdatedAtUtc, p.UpdatedBy!.FullName)))
            .FirstOrDefaultAsync(ct)
        ?? throw NotFound();

    public async Task<ProductDetailDto> CreateAsync(SaveProductRequest request, CancellationToken ct = default)
    {
        var product = new Product();
        await ApplyAsync(product, request, ct);
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        return await GetAsync(product.Id, ct);
    }

    public async Task<ProductDetailDto> UpdateAsync(int id, SaveProductRequest request, CancellationToken ct = default)
    {
        var product = await db.Products.Include(p => p.BranchSettings).FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw NotFound();
        await ApplyAsync(product, request, ct);
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task<ProductDetailDto> SetStatusAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw NotFound();
        product.IsActive = isActive;
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    private async Task ApplyAsync(Product product, SaveProductRequest request, CancellationToken ct)
    {
        var isNew = product.Id == 0;
        var errors = new ValidationErrors();

        var price = request.Price!.Value;
        var cost = request.Cost!.Value;
        ValidateMoney(errors, "cost", "El costo", cost);
        ValidateMoney(errors, "price", "El precio", price);
        if (!errors.Has("cost") && !errors.Has("price") && price <= cost)
            errors.Add("price", "El precio debe ser mayor al costo.");
        if (request.MinStock < 0)
            errors.Add("minStock", "El stock mínimo no puede ser negativo.");

        await ValidateReferencesAsync(product, request, isNew, errors, ct);
        var branchRows = await ValidateBranchesAsync(product, request, cost, errors, ct);
        errors.ThrowIfAny();

        var name = TextNormalizer.CapitalizeFirst(request.Name);
        var presentation = TextNormalizer.CapitalizeFirst(request.Presentation);
        var barcode = TextNormalizer.TrimToNull(request.Barcode);
        await EnsureUniqueAsync(name, presentation, barcode, excludeId: isNew ? null : product.Id, ct);

        product.Name = name;
        product.Presentation = presentation;
        product.ActiveIngredient = TextNormalizer.CapitalizeFirstOrNull(request.ActiveIngredient);
        product.TherapeuticAction = TextNormalizer.CapitalizeFirstOrNull(request.TherapeuticAction);
        product.Indications = TextNormalizer.CapitalizeFirstOrNull(request.Indications);
        product.Contraindications = TextNormalizer.CapitalizeFirstOrNull(request.Contraindications);
        product.Notes = TextNormalizer.CapitalizeFirstOrNull(request.Notes);
        product.Measure = TextNormalizer.CapitalizeFirstOrNull(request.Measure);
        product.Laboratory = TextNormalizer.CapitalizeFirstOrNull(request.Laboratory);
        product.Barcode = barcode;
        product.CategoryId = request.CategoryId!.Value;
        product.SupplierId = request.SupplierId!.Value;
        product.Price = price;
        product.Cost = cost;
        product.MinStock = request.MinStock;
        product.IsControlled = request.IsControlled;
        product.IsBonusEligible = request.IsBonusEligible;

        ApplyBranchSettings(product, branchRows);
    }

    private async Task ValidateReferencesAsync(
        Product product, SaveProductRequest request, bool isNew, ValidationErrors errors, CancellationToken ct)
    {
        var categoryId = request.CategoryId!.Value;
        var category = await db.Categories.AsNoTracking()
            .Where(c => c.Id == categoryId).Select(c => new { c.IsActive }).FirstOrDefaultAsync(ct);
        if (category is null)
            errors.Add("categoryId", "La categoría no existe.");
        else if (!category.IsActive && (isNew || product.CategoryId != categoryId))
            errors.Add("categoryId", "La categoría está inactiva.");

        var supplierId = request.SupplierId!.Value;
        var supplier = await db.Suppliers.AsNoTracking()
            .Where(s => s.Id == supplierId).Select(s => new { s.IsActive }).FirstOrDefaultAsync(ct);
        if (supplier is null)
            errors.Add("supplierId", "El proveedor no existe.");
        else if (!supplier.IsActive && (isNew || product.SupplierId != supplierId))
            errors.Add("supplierId", "El proveedor está inactivo.");
    }

    /// <summary>
    /// Validates the branch rows and returns those that carry data. A row with no price, minimum
    /// stock or shelf means "use the defaults" and is not stored.
    /// </summary>
    private async Task<List<ProductBranchRequest>> ValidateBranchesAsync(
        Product product, SaveProductRequest request, decimal cost, ValidationErrors errors, CancellationToken ct)
    {
        var rows = request.Branches
            .Where(b => b.Price is not null || b.MinStock is not null || TextNormalizer.TrimToNull(b.Shelf) is not null)
            .ToList();

        if (rows.GroupBy(r => r.BranchId!.Value).Any(g => g.Count() > 1))
            errors.Add("branches", "Hay sucursales repetidas.");

        var ids = rows.Select(r => r.BranchId!.Value).Distinct().ToList();
        var branches = await db.Branches.AsNoTracking()
            .Where(b => ids.Contains(b.Id))
            .Select(b => new { b.Id, b.Name, b.IsActive })
            .ToDictionaryAsync(b => b.Id, ct);

        foreach (var row in rows.DistinctBy(r => r.BranchId))
        {
            var branchId = row.BranchId!.Value;
            if (!branches.TryGetValue(branchId, out var branch))
            {
                errors.Add("branches", $"La sucursal {branchId} no existe.");
                continue;
            }

            var hadSettings = product.BranchSettings.Any(s => s.BranchId == branchId);
            if (!branch.IsActive && !hadSettings)
                errors.Add("branches", $"La sucursal {branch.Name} está inactiva.");

            if (row.Price is { } branchPrice)
            {
                var before = errors.Count;
                ValidateMoney(errors, "branches", $"Sucursal {branch.Name}: el precio", branchPrice);
                if (errors.Count == before && branchPrice <= cost)
                    errors.Add("branches", $"Sucursal {branch.Name}: el precio debe ser mayor al costo.");
            }

            if (row.MinStock < 0)
                errors.Add("branches", $"Sucursal {branch.Name}: el stock mínimo no puede ser negativo.");
        }

        return rows;
    }

    private static void ValidateMoney(ValidationErrors errors, string field, string label, decimal value)
    {
        if (value <= 0)
            errors.Add(field, $"{label} debe ser mayor a 0.");
        else if (value > MaxMoney)
            errors.Add(field, $"{label} no puede superar {MaxMoney:N2}.");
        else if (decimal.Round(value, 2) != value)
            errors.Add(field, $"{label} admite como máximo 2 decimales.");
    }

    /// <summary>Makes the product's branch settings match the requested rows (add, update, remove).</summary>
    private static void ApplyBranchSettings(Product product, List<ProductBranchRequest> rows)
    {
        var requestedIds = rows.Select(r => r.BranchId!.Value).ToHashSet();
        product.BranchSettings.RemoveAll(s => !requestedIds.Contains(s.BranchId));

        foreach (var row in rows)
        {
            var branchId = row.BranchId!.Value;
            var setting = product.BranchSettings.FirstOrDefault(s => s.BranchId == branchId);
            if (setting is null)
            {
                setting = new ProductBranchSetting { BranchId = branchId };
                product.BranchSettings.Add(setting);
            }

            setting.Price = row.Price;
            setting.MinStock = row.MinStock;
            setting.Shelf = TextNormalizer.TrimToNull(row.Shelf);
        }
    }

    /// <summary>
    /// Name, presentation and barcode compare without case or accents (the name columns use a
    /// case and accent insensitive collation). Several products may have no barcode.
    /// </summary>
    private async Task EnsureUniqueAsync(string name, string presentation, string? barcode, int? excludeId, CancellationToken ct)
    {
        if (await db.Products.AnyAsync(
                p => p.Name == name && p.Presentation == presentation && (excludeId == null || p.Id != excludeId), ct))
            throw new ConflictException($"Ya existe el producto '{name}' con la presentación '{presentation}'.");

        if (barcode is not null
            && await db.Products.AnyAsync(p => p.Barcode == barcode && (excludeId == null || p.Id != excludeId), ct))
            throw new ConflictException($"Ya existe un producto con el código de barras '{barcode}'.");
    }

    private static NotFoundException NotFound() => new("El producto no existe.");

    private sealed class ValidationErrors
    {
        private readonly Dictionary<string, List<string>> _errors = [];

        public int Count => _errors.Values.Sum(e => e.Count);

        public bool Has(string field) => _errors.ContainsKey(field);

        public void Add(string field, string message)
        {
            if (!_errors.TryGetValue(field, out var list))
                _errors[field] = list = [];
            list.Add(message);
        }

        public void ThrowIfAny()
        {
            if (_errors.Count > 0)
                throw new ValidationException(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }
    }
}
