using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Pharmacy.Api.Auth;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;
using Pharmacy.Core.Settings;

namespace Pharmacy.Api.Controllers;

/// <summary>
/// Company data and logo. Reading is open to every authenticated user (tickets and reports need it);
/// changes require <see cref="Permissions.SettingsManage"/>.
/// </summary>
[ApiController]
[Route("api/settings/company")]
public sealed class CompanySettingsController(CompanySettingsService settingsService) : ControllerBase
{
    /// <summary>Upper bound for the whole multipart request; the service enforces the real logo limit.</summary>
    private const long MaxLogoRequestBytes = CompanySettingsService.MaxLogoBytes + 64 * 1024;

    [HttpGet]
    public Task<CompanySettingsDto> Get(CancellationToken ct) => settingsService.GetAsync(ct);

    [HttpPut]
    [HasPermission(Permissions.SettingsManage)]
    public Task<CompanySettingsDto> Update(SaveCompanySettingsRequest request, CancellationToken ct) =>
        settingsService.UpdateAsync(request, ct);

    [HttpGet("logo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    public async Task<IActionResult> GetLogo(CancellationToken ct)
    {
        var logo = await settingsService.GetLogoAsync(ct);
        var etag = new EntityTagHeaderValue($"\"{logo.UpdatedAtUtc.Ticks}\"");
        Response.Headers.CacheControl = "private, no-cache";
        return File(logo.Content, logo.ContentType, lastModified: null, entityTag: etag);
    }

    [HttpPut("logo")]
    [HasPermission(Permissions.SettingsManage)]
    [RequestSizeLimit(MaxLogoRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxLogoRequestBytes)]
    public async Task<CompanySettingsDto> UploadLogo(IFormFile? logo, CancellationToken ct)
    {
        if (logo is null)
            throw new ValidationException("logo", "Debe seleccionar una imagen.");

        await using var stream = logo.OpenReadStream();
        return await settingsService.SetLogoAsync(stream, ct);
    }

    [HttpDelete("logo")]
    [HasPermission(Permissions.SettingsManage)]
    public Task<CompanySettingsDto> RemoveLogo(CancellationToken ct) => settingsService.RemoveLogoAsync(ct);
}
