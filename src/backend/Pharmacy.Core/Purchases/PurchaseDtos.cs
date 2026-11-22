using System.ComponentModel.DataAnnotations;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.Purchases;

public sealed record PurchaseListItemDto(
    int Id,
    DateOnly PurchaseDate,
    string DocumentNumber,
    NamedReference Supplier,
    NamedReference Branch,
    PurchaseType Type,
    string? PaymentMethod,
    DateOnly? PaymentDate,
    decimal Total,
    bool IsCancelled,
    string? CreatedBy);

/// <summary>
/// Line of a purchase. <see cref="CostAtOrAboveSalePrice"/> is the non-blocking warning of P-84: the
/// purchase cost is not lower than the current sale price in the branch.
/// </summary>
public sealed record PurchaseItemDto(
    int ProductId,
    string ProductName,
    string Presentation,
    int Quantity,
    decimal UnitCost,
    decimal Subtotal,
    decimal SalePrice,
    bool CostAtOrAboveSalePrice);

public sealed record AccountPayableDto(
    int Id,
    DateOnly StartDate,
    DateOnly DueDate,
    decimal Total,
    decimal Balance,
    AccountPayableStatus Status,
    bool HasPayments);

public sealed record PurchaseDetailDto(
    int Id,
    DateOnly PurchaseDate,
    string DocumentNumber,
    NamedReference Supplier,
    NamedReference Branch,
    PurchaseType Type,
    NamedReference? PaymentMethod,
    DateOnly? PaymentDate,
    int? CreditDays,
    decimal Total,
    bool IsCancelled,
    DateTime? CancelledAtUtc,
    string? CancelledBy,
    string? CancellationReason,
    IReadOnlyList<PurchaseItemDto> Items,
    AccountPayableDto? AccountPayable,
    AuditInfo Audit);

public sealed class PurchaseQuery
{
    /// <summary>Part of the invoice number.</summary>
    [StringLength(30)]
    public string? Search { get; set; }

    public int? BranchId { get; set; }
    public int? SupplierId { get; set; }
    public PurchaseType? Type { get; set; }

    /// <summary>Null returns every purchase; true only cancelled; false only the valid ones.</summary>
    public bool? IsCancelled { get; set; }

    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class SavePurchaseItemRequest
{
    [Required(ErrorMessage = "El producto es obligatorio.")]
    public int? ProductId { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    public int? Quantity { get; set; }

    [Required(ErrorMessage = "El costo es obligatorio.")]
    public decimal? UnitCost { get; set; }
}

public sealed class SavePurchaseRequest
{
    [Required(ErrorMessage = "La fecha de la compra es obligatoria.")]
    public DateOnly? PurchaseDate { get; set; }

    [Required(ErrorMessage = "La sucursal es obligatoria.")]
    public int? BranchId { get; set; }

    [Required(ErrorMessage = "El proveedor es obligatorio.")]
    public int? SupplierId { get; set; }

    [Required(ErrorMessage = "El número de documento es obligatorio.")]
    [StringLength(30, ErrorMessage = "El número de documento no puede superar 30 caracteres.")]
    public string DocumentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de compra es obligatorio.")]
    public PurchaseType? Type { get; set; }

    /// <summary>Only for cash purchases.</summary>
    public int? PaymentMethodId { get; set; }

    /// <summary>Only for credit purchases.</summary>
    public int? CreditDays { get; set; }

    public List<SavePurchaseItemRequest> Items { get; set; } = [];
}

public sealed class CancelPurchaseRequest
{
    [Required(ErrorMessage = "El motivo de la anulación es obligatorio.")]
    [StringLength(250, ErrorMessage = "El motivo no puede superar 250 caracteres.")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Product offered when registering a purchase (equivalent to sp_infoProdCompras).</summary>
public sealed record PurchaseProductDto(
    int Id,
    string Name,
    string Presentation,
    string? Laboratory,
    NamedReference Category,
    NamedReference Supplier,
    int Stock,
    decimal Cost,
    decimal SalePrice);

public sealed class PurchaseProductQuery
{
    [Required(ErrorMessage = "La sucursal es obligatoria.")]
    public int? BranchId { get; set; }

    [StringLength(100)]
    public string? Search { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
