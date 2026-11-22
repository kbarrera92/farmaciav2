using Pharmacy.Core.Common;

namespace Pharmacy.Core.PaymentMethods;

/// <summary>Payment method used by purchases (equivalent to FORMAPAGO in the original system).</summary>
public class PaymentMethod : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
