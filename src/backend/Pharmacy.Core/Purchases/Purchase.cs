using System.Text.Json.Serialization;
using Pharmacy.Core.Branches;
using Pharmacy.Core.Common;
using Pharmacy.Core.PaymentMethods;
using Pharmacy.Core.Products;
using Pharmacy.Core.Suppliers;
using Pharmacy.Core.Users;

namespace Pharmacy.Core.Purchases;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PurchaseType
{
    Cash = 1,
    Credit = 2,
}

/// <summary>
/// Purchase from a supplier received in a branch (equivalent to COMPRA). A credit purchase has an
/// <see cref="AccountPayable"/>. A cancelled purchase is kept, marked, and no longer affects stock.
/// </summary>
public class Purchase : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>Date of the purchase (no time).</summary>
    public DateOnly PurchaseDate { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    /// <summary>Supplier invoice number. Unique per supplier among non-cancelled purchases.</summary>
    public string DocumentNumber { get; set; } = string.Empty;

    public PurchaseType Type { get; set; }

    /// <summary>Only for cash purchases.</summary>
    public int? PaymentMethodId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }

    /// <summary>Cash: the purchase date. Credit: date the account payable is settled (empty until then).</summary>
    public DateOnly? PaymentDate { get; set; }

    /// <summary>Only for credit purchases.</summary>
    public int? CreditDays { get; set; }

    public decimal Total { get; set; }

    public bool IsCancelled { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public int? CancelledByUserId { get; set; }
    public User? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }

    public List<PurchaseItem> Items { get; set; } = [];
    public AccountPayable? AccountPayable { get; set; }
}

/// <summary>Line of a purchase (equivalent to DETALLECOMPRA).</summary>
public class PurchaseItem
{
    public int Id { get; set; }

    public int PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Subtotal { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccountPayableStatus
{
    Pending = 1,
    Paid = 2,
}

/// <summary>
/// Debt created by a credit purchase (equivalent to CUENTAXPAGAR). Payments belong to the accounts
/// payable module; until then a payment exists when <see cref="Balance"/> differs from <see cref="Total"/>.
/// </summary>
public class AccountPayable : AuditableEntity
{
    public int Id { get; set; }

    public int PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Total { get; set; }
    public decimal Balance { get; set; }
    public AccountPayableStatus Status { get; set; } = AccountPayableStatus.Pending;
    public DateOnly? PaidDate { get; set; }

    public bool HasPayments => Balance != Total;
}
