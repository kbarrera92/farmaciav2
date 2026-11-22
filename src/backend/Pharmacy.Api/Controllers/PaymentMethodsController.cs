using Microsoft.AspNetCore.Mvc;
using Pharmacy.Api.Auth;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;
using Pharmacy.Core.PaymentMethods;

namespace Pharmacy.Api.Controllers;

[ApiController]
[Route("api/payment-methods")]
public sealed class PaymentMethodsController(PaymentMethodService paymentMethodService) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.SettingsView)]
    public Task<IReadOnlyList<PaymentMethodDto>> List([FromQuery] bool includeInactive, CancellationToken ct) =>
        paymentMethodService.ListAsync(includeInactive, ct);

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.SettingsView)]
    public Task<PaymentMethodDto> Get(int id, CancellationToken ct) => paymentMethodService.GetAsync(id, ct);

    [HttpPost]
    [HasPermission(Permissions.SettingsManage)]
    [ProducesResponseType<PaymentMethodDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PaymentMethodDto>> Create(SavePaymentMethodRequest request, CancellationToken ct)
    {
        var paymentMethod = await paymentMethodService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = paymentMethod.Id }, paymentMethod);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.SettingsManage)]
    public Task<PaymentMethodDto> Update(int id, SavePaymentMethodRequest request, CancellationToken ct) =>
        paymentMethodService.UpdateAsync(id, request, ct);

    [HttpPatch("{id:int}/status")]
    [HasPermission(Permissions.SettingsManage)]
    public Task<PaymentMethodDto> SetStatus(int id, SetStatusRequest request, CancellationToken ct) =>
        paymentMethodService.SetStatusAsync(id, request.IsActive!.Value, ct);
}
