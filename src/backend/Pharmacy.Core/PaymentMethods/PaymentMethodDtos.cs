using System.ComponentModel.DataAnnotations;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.PaymentMethods;

public sealed record PaymentMethodDto(int Id, string Name, bool IsActive, AuditInfo Audit);

public sealed class SavePaymentMethodRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede superar 50 caracteres.")]
    public string Name { get; set; } = string.Empty;
}
