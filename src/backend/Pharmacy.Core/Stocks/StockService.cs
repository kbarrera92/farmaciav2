using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.Stocks;

/// <summary>
/// Existence of a product in a branch. <see cref="MinStock"/> is the branch value when it has its own,
/// otherwise the product default. A product without a stock row shows 0.
/// </summary>
public sealed record StockListItemDto(
    int ProductId,
    string Name,
    string Presentation,
    NamedReference Category,
    int Quantity,
    int? MinStock,
    bool IsActive);

public sealed class StockQuery
{
    [Required(ErrorMessage = "La sucursal es obligatoria.")]
    public int? BranchId { get; set; }

    public int? CategoryId { get; set; }

    [StringLength(100)]
    public string? Search { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class StockService(IAppDbContext db)
{
    public async Task<PagedResult<StockListItemDto>> ListAsync(StockQuery query, CancellationToken ct = default)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        var branchId = query.BranchId!.Value;
        if (!await db.Branches.AnyAsync(b => b.Id == branchId, ct))
            throw new NotFoundException("La sucursal no existe.");

        var products = db.Products.AsNoTracking();
        if (query.CategoryId is { } categoryId)
            products = products.Where(p => p.CategoryId == categoryId);
        if (TextNormalizer.TrimToNull(query.Search) is { } search)
        {
            products = products.Where(p =>
                p.Name.Contains(search)
                || p.Presentation.Contains(search)
                || (p.Barcode != null && p.Barcode.Contains(search)));
        }

        var total = await products.CountAsync(ct);
        var items = await products
            .OrderBy(p => p.Name).ThenBy(p => p.Presentation).ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new StockListItemDto(
                p.Id, p.Name, p.Presentation,
                new NamedReference(p.CategoryId, p.Category!.Name),
                db.Stocks.Where(s => s.BranchId == branchId && s.ProductId == p.Id).Select(s => s.Quantity).FirstOrDefault(),
                p.BranchSettings.Where(s => s.BranchId == branchId).Select(s => s.MinStock).FirstOrDefault() ?? p.MinStock,
                p.IsActive))
            .ToListAsync(ct);

        return new PagedResult<StockListItemDto>(items, page, pageSize, total);
    }
}
