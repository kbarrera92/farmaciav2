using Pharmacy.Core.Common;

namespace Pharmacy.Core.Categories;

/// <summary>Product category (equivalent to CATEGORIA in the original system).</summary>
public class Category : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
