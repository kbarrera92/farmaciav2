using Pharmacy.Core.Branches;
using Pharmacy.Core.Categories;
using Pharmacy.Core.Common;
using Pharmacy.Core.Suppliers;

namespace Pharmacy.Core.Products;

/// <summary>
/// Catalog product (equivalent to PRODUCTO in the original system). Price and minimum stock are the
/// defaults; each branch may override them (and set its own shelf) through <see cref="BranchSettings"/>.
/// </summary>
public class Product : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Presentation { get; set; } = string.Empty;
    public string? ActiveIngredient { get; set; }
    public string? TherapeuticAction { get; set; }
    public string? Indications { get; set; }
    public string? Contraindications { get; set; }
    public string? Notes { get; set; }
    public string? Measure { get; set; }
    public string? Laboratory { get; set; }

    /// <summary>Unique when informed; null otherwise.</summary>
    public string? Barcode { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    /// <summary>Default sale price, used by every branch without its own price.</summary>
    public decimal Price { get; set; }

    public decimal Cost { get; set; }

    /// <summary>Default minimum stock, used by every branch without its own value.</summary>
    public int? MinStock { get; set; }

    public bool IsControlled { get; set; }
    public bool IsBonusEligible { get; set; }
    public bool IsActive { get; set; } = true;

    public List<ProductBranchSetting> BranchSettings { get; set; } = [];
}

/// <summary>Branch-specific values of a product. A null value means "use the product default".</summary>
public class ProductBranchSetting
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public decimal? Price { get; set; }
    public int? MinStock { get; set; }

    /// <summary>Free-text location of the product in the branch (e.g. "A-3", "Vitrina 2").</summary>
    public string? Shelf { get; set; }
}
