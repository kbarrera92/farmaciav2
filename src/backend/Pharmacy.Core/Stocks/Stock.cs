using Pharmacy.Core.Branches;
using Pharmacy.Core.Products;

namespace Pharmacy.Core.Stocks;

/// <summary>
/// Existence of a product in a branch (equivalent to Existencias). The row is created by the first
/// movement; no row means 0. Negative values are allowed (RN-62).
/// </summary>
public class Stock
{
    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
