using System.ComponentModel.DataAnnotations;

namespace Pharmacy.Core.Common;

/// <summary>Request body to activate or deactivate a record.</summary>
public sealed class SetStatusRequest
{
    [Required(ErrorMessage = "Debe indicar si el registro queda activo.")]
    public bool? IsActive { get; set; }
}
