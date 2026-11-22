using Pharmacy.Core.Common;

namespace Pharmacy.Core.Suppliers;

/// <summary>Product supplier (equivalent to PROVEEDOR in the original system).</summary>
public class Supplier : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
}
