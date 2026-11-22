using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Pharmacy.Core.Auth;
using Pharmacy.Core.PaymentMethods;
using Pharmacy.Core.Settings;

namespace Pharmacy.IntegrationTests;

public sealed class SettingsApiTests : IDisposable
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 9, 8, 7, 6];

    private readonly PharmacyApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Logo_upload_round_trip_through_multipart()
    {
        var admin = await _factory.CreateAdminClientAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/settings/company/logo")).StatusCode);

        (await admin.PutAsJsonAsync("/api/settings/company", new { name = "Farmacia Ahorro" })).EnsureSuccessStatusCode();
        var upload = await admin.PutAsync("/api/settings/company/logo", LogoForm(Png, "image/gif"));
        upload.EnsureSuccessStatusCode();
        Assert.True((await upload.Content.ReadFromJsonAsync<CompanySettingsDto>())!.HasLogo);

        var logo = await admin.GetAsync("/api/settings/company/logo");
        Assert.Equal(HttpStatusCode.OK, logo.StatusCode);
        Assert.Equal("image/png", logo.Content.Headers.ContentType!.MediaType);
        Assert.Equal(Png, await logo.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Non_image_upload_returns_validation_problem()
    {
        var admin = await _factory.CreateAdminClientAsync();
        (await admin.PutAsJsonAsync("/api/settings/company", new { name = "Farmacia Ahorro" })).EnsureSuccessStatusCode();

        var response = await admin.PutAsync("/api/settings/company/logo", LogoForm("hola"u8.ToArray(), "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Any_authenticated_user_can_read_company_data_but_only_managers_can_change_it()
    {
        var admin = await _factory.CreateAdminClientAsync();
        (await admin.PutAsJsonAsync("/api/settings/company", new { name = "Farmacia Ahorro" })).EnsureSuccessStatusCode();
        (await admin.PutAsync("/api/settings/company/logo", LogoForm(Png, "image/png"))).EnsureSuccessStatusCode();
        var (viewer, _) = await _factory.CreateUserClientAsync(admin, "consulta", Permissions.SettingsView);

        var company = await viewer.GetFromJsonAsync<CompanySettingsDto>("/api/settings/company");
        Assert.Equal("Farmacia Ahorro", company!.Name);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/settings/company/logo")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/payment-methods")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.PutAsJsonAsync("/api/settings/company", new { name = "Otra" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.PutAsync("/api/settings/company/logo", LogoForm(Png, "image/png"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.DeleteAsync("/api/settings/company/logo")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await viewer.PostAsJsonAsync("/api/payment-methods", new { name = "Contado" })).StatusCode);
    }

    [Fact]
    public async Task Payment_methods_require_the_settings_view_permission()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var (other, _) = await _factory.CreateUserClientAsync(admin, "sinpermiso", Permissions.BranchesView);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync("/api/payment-methods")).StatusCode);
    }

    [Fact]
    public async Task Audit_fields_record_the_user_of_the_request()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var (manager, _) = await _factory.CreateUserClientAsync(admin, "gestor", Permissions.SettingsManage);

        var created = await (await admin.PostAsJsonAsync("/api/payment-methods", new { name = "Contado" }))
            .Content.ReadFromJsonAsync<PaymentMethodDto>();
        var updated = await (await manager.PutAsJsonAsync($"/api/payment-methods/{created!.Id}", new { name = "Efectivo" }))
            .Content.ReadFromJsonAsync<PaymentMethodDto>();

        Assert.Equal("Administrador", created.Audit.CreatedBy);
        Assert.Equal("Administrador", updated!.Audit.CreatedBy);
        Assert.Equal("Usuario gestor", updated.Audit.UpdatedBy);
    }

    private static MultipartFormDataContent LogoForm(byte[] bytes, string declaredType)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(declaredType);
        return new MultipartFormDataContent { { file, "logo", "logo.png" } };
    }
}
