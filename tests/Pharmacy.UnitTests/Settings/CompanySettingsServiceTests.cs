using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Common;
using Pharmacy.Core.Settings;
using Pharmacy.UnitTests.TestSupport;

namespace Pharmacy.UnitTests.Settings;

public sealed class CompanySettingsServiceTests : IDisposable
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4];

    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public async Task Not_configured_company_returns_empty_data()
    {
        var settings = await _env.CompanySettings.GetAsync();

        Assert.False(settings.IsConfigured);
        Assert.False(settings.HasLogo);
        Assert.Null(settings.Audit);
    }

    [Fact]
    public async Task Saving_twice_keeps_a_single_row()
    {
        await _env.CompanySettings.UpdateAsync(new SaveCompanySettingsRequest { Name = " Farmacia Ahorro ", TaxId = " " });
        var updated = await _env.CompanySettings.UpdateAsync(new SaveCompanySettingsRequest
        {
            Name = "Farmacia del Ahorro",
            TaxId = "1234567-8",
            Address = "Zona 1",
        });

        Assert.True(updated.IsConfigured);
        Assert.Equal("Farmacia Del Ahorro", updated.Name);
        Assert.Equal("1234567-8", updated.TaxId);
        Assert.Equal(1, await _env.Db.CompanySettings.CountAsync());
    }

    [Fact]
    public async Task Logo_requires_the_company_data_first()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _env.CompanySettings.SetLogoAsync(new MemoryStream(Png)));
    }

    [Theory]
    [InlineData("png", "image/png")]
    [InlineData("jpeg", "image/jpeg")]
    public async Task Png_and_jpeg_logos_are_stored_with_the_detected_type(string kind, string expectedType)
    {
        await ConfigureCompanyAsync();
        var bytes = kind == "png" ? Png : Jpeg;

        var settings = await _env.CompanySettings.SetLogoAsync(new MemoryStream(bytes));
        var logo = await _env.CompanySettings.GetLogoAsync();

        Assert.True(settings.HasLogo);
        Assert.Equal(_env.Clock.Now.UtcDateTime, settings.LogoUpdatedAtUtc);
        Assert.Equal(bytes, logo.Content);
        Assert.Equal(expectedType, logo.ContentType);
    }

    [Fact]
    public async Task Files_that_are_not_png_or_jpeg_are_rejected_by_content()
    {
        await ConfigureCompanyAsync();
        var fakeImage = "<svg onload=alert(1)>"u8.ToArray();

        var error = await Assert.ThrowsAsync<ValidationException>(
            () => _env.CompanySettings.SetLogoAsync(new MemoryStream(fakeImage)));
        Assert.Contains("logo", error.Errors.Keys);
    }

    [Fact]
    public async Task Logo_larger_than_the_limit_is_rejected()
    {
        await ConfigureCompanyAsync();
        var tooLarge = new byte[CompanySettingsService.MaxLogoBytes + 1];
        Png.CopyTo(tooLarge, 0);

        await Assert.ThrowsAsync<ValidationException>(() => _env.CompanySettings.SetLogoAsync(new MemoryStream(tooLarge)));

        var limit = new byte[CompanySettingsService.MaxLogoBytes];
        Png.CopyTo(limit, 0);
        var settings = await _env.CompanySettings.SetLogoAsync(new MemoryStream(limit));
        Assert.True(settings.HasLogo);
    }

    [Fact]
    public async Task Removed_logo_is_no_longer_available()
    {
        await ConfigureCompanyAsync();
        await _env.CompanySettings.SetLogoAsync(new MemoryStream(Png));

        var settings = await _env.CompanySettings.RemoveLogoAsync();

        Assert.False(settings.HasLogo);
        await Assert.ThrowsAsync<NotFoundException>(() => _env.CompanySettings.GetLogoAsync());
    }

    private Task<CompanySettingsDto> ConfigureCompanyAsync() =>
        _env.CompanySettings.UpdateAsync(new SaveCompanySettingsRequest { Name = "Farmacia Ahorro" });
}
