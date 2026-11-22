using System.ComponentModel.DataAnnotations;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.Products;

public sealed record ProductListItemDto(
    int Id,
    string Name,
    string Presentation,
    string? Barcode,
    NamedReference Category,
    NamedReference Supplier,
    decimal Price,
    decimal Cost,
    bool IsControlled,
    bool IsBonusEligible,
    bool IsActive);

/// <summary>Branch-specific values; null means the branch uses the product default.</summary>
public sealed record ProductBranchDto(int BranchId, string BranchName, decimal? Price, int? MinStock, string? Shelf);

public sealed record ProductDetailDto(
    int Id,
    string Name,
    string Presentation,
    string? ActiveIngredient,
    string? TherapeuticAction,
    string? Indications,
    string? Contraindications,
    string? Notes,
    string? Measure,
    string? Laboratory,
    string? Barcode,
    NamedReference Category,
    NamedReference Supplier,
    decimal Price,
    decimal Cost,
    int? MinStock,
    bool IsControlled,
    bool IsBonusEligible,
    bool IsActive,
    IReadOnlyList<ProductBranchDto> Branches,
    AuditInfo Audit);

public sealed class ProductQuery
{
    [StringLength(100)]
    public string? Search { get; set; }

    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsControlled { get; set; }
    public bool? IsBonusEligible { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class ProductBranchRequest
{
    [Required(ErrorMessage = "La sucursal es obligatoria.")]
    public int? BranchId { get; set; }

    public decimal? Price { get; set; }

    public int? MinStock { get; set; }

    [StringLength(30, ErrorMessage = "La estantería no puede superar 30 caracteres.")]
    public string? Shelf { get; set; }
}

public sealed class SaveProductRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar 150 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La presentación es obligatoria.")]
    [StringLength(100, ErrorMessage = "La presentación no puede superar 100 caracteres.")]
    public string Presentation { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "La composición no puede superar 150 caracteres.")]
    public string? ActiveIngredient { get; set; }

    [StringLength(150, ErrorMessage = "La acción terapéutica no puede superar 150 caracteres.")]
    public string? TherapeuticAction { get; set; }

    [StringLength(150, ErrorMessage = "Las indicaciones no pueden superar 150 caracteres.")]
    public string? Indications { get; set; }

    [StringLength(150, ErrorMessage = "Las contraindicaciones no pueden superar 150 caracteres.")]
    public string? Contraindications { get; set; }

    [StringLength(250, ErrorMessage = "Las observaciones no pueden superar 250 caracteres.")]
    public string? Notes { get; set; }

    [StringLength(75, ErrorMessage = "La medida no puede superar 75 caracteres.")]
    public string? Measure { get; set; }

    [StringLength(100, ErrorMessage = "El laboratorio no puede superar 100 caracteres.")]
    public string? Laboratory { get; set; }

    [StringLength(25, ErrorMessage = "El código de barras no puede superar 25 caracteres.")]
    public string? Barcode { get; set; }

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "El proveedor es obligatorio.")]
    public int? SupplierId { get; set; }

    [Required(ErrorMessage = "El precio es obligatorio.")]
    public decimal? Price { get; set; }

    [Required(ErrorMessage = "El costo es obligatorio.")]
    public decimal? Cost { get; set; }

    public int? MinStock { get; set; }

    public bool IsControlled { get; set; }

    public bool IsBonusEligible { get; set; }

    public List<ProductBranchRequest> Branches { get; set; } = [];
}
