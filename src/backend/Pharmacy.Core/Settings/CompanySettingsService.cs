using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.Settings;

public sealed class CompanySettingsService(IAppDbContext db, TimeProvider timeProvider)
{
    public const int MaxLogoBytes = 512 * 1024;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];

    /// <summary>Company data without the logo bytes; empty values when it was never configured.</summary>
    public async Task<CompanySettingsDto> GetAsync(CancellationToken ct = default)
    {
        return await db.CompanySettings.AsNoTracking()
            .Where(c => c.Id == CompanySettings.SingletonId)
            .Select(c => new CompanySettingsDto(
                true, c.Name, c.TaxId, c.Address, c.Phone, c.Email,
                c.Logo != null, c.LogoUpdatedAtUtc,
                new AuditInfo(c.CreatedAtUtc, c.CreatedBy!.FullName, c.UpdatedAtUtc, c.UpdatedBy!.FullName)))
            .FirstOrDefaultAsync(ct)
            ?? new CompanySettingsDto(false, string.Empty, null, null, null, null, false, null, null);
    }

    public async Task<CompanySettingsDto> UpdateAsync(SaveCompanySettingsRequest request, CancellationToken ct = default)
    {
        var settings = await db.CompanySettings.FirstOrDefaultAsync(c => c.Id == CompanySettings.SingletonId, ct);
        if (settings is null)
        {
            settings = new CompanySettings();
            db.CompanySettings.Add(settings);
        }

        settings.Name = request.Name.Trim();
        settings.TaxId = TextNormalizer.TrimToNull(request.TaxId);
        settings.Address = TextNormalizer.TrimToNull(request.Address);
        settings.Phone = TextNormalizer.TrimToNull(request.Phone);
        settings.Email = TextNormalizer.TrimToNull(request.Email);
        await db.SaveChangesAsync(ct);

        return await GetAsync(ct);
    }

    /// <summary>
    /// Replaces the logo. The image type is detected from the file content (PNG or JPEG); the
    /// name or content type sent by the client are not trusted.
    /// </summary>
    public async Task<CompanySettingsDto> SetLogoAsync(Stream content, CancellationToken ct = default)
    {
        var bytes = await ReadLimitedAsync(content, ct);
        if (bytes.Length == 0)
            throw new ValidationException("logo", "Debe seleccionar una imagen.");

        var contentType = DetectImageType(bytes)
            ?? throw new ValidationException("logo", "El logo debe ser una imagen PNG o JPEG.");

        var settings = await db.CompanySettings.FirstOrDefaultAsync(c => c.Id == CompanySettings.SingletonId, ct)
            ?? throw new BusinessRuleException("Guarde primero los datos de la empresa.");

        settings.Logo = bytes;
        settings.LogoContentType = contentType;
        settings.LogoUpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return await GetAsync(ct);
    }

    public async Task<CompanyLogo> GetLogoAsync(CancellationToken ct = default)
    {
        var logo = await db.CompanySettings.AsNoTracking()
            .Where(c => c.Id == CompanySettings.SingletonId && c.Logo != null)
            .Select(c => new CompanyLogo(c.Logo!, c.LogoContentType!, c.LogoUpdatedAtUtc!.Value))
            .FirstOrDefaultAsync(ct);
        return logo ?? throw new NotFoundException("La empresa no tiene logo configurado.");
    }

    public async Task<CompanySettingsDto> RemoveLogoAsync(CancellationToken ct = default)
    {
        var settings = await db.CompanySettings.FirstOrDefaultAsync(c => c.Id == CompanySettings.SingletonId, ct);
        if (settings?.Logo is not null)
        {
            settings.Logo = null;
            settings.LogoContentType = null;
            settings.LogoUpdatedAtUtc = null;
            await db.SaveChangesAsync(ct);
        }

        return await GetAsync(ct);
    }

    private static async Task<byte[]> ReadLimitedAsync(Stream content, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > MaxLogoBytes)
                throw new ValidationException("logo", $"El logo no puede superar {MaxLogoBytes / 1024} KB.");
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }

    private static string? DetectImageType(byte[] bytes)
    {
        if (bytes.AsSpan().StartsWith(PngSignature)) return "image/png";
        if (bytes.AsSpan().StartsWith(JpegSignature)) return "image/jpeg";
        return null;
    }
}
