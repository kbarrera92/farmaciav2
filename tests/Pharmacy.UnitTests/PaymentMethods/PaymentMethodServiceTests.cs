using Pharmacy.Core.Common;
using Pharmacy.Core.PaymentMethods;
using Pharmacy.UnitTests.TestSupport;

namespace Pharmacy.UnitTests.PaymentMethods;

public sealed class PaymentMethodServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public async Task Name_must_be_unique_ignoring_case_and_spaces()
    {
        await Create("Contado");

        await Assert.ThrowsAsync<ConflictException>(() => Create(" CONTADO "));
    }

    [Fact]
    public async Task Name_must_be_unique_ignoring_accents()
    {
        await Create("Crédito");

        await Assert.ThrowsAsync<ConflictException>(() => Create("CREDITO"));
    }

    [Fact]
    public async Task Renaming_keeping_the_same_name_is_allowed_but_not_to_another_existing_name()
    {
        var cash = await Create("Contado");
        var credit = await Create("Crédito");

        var renamed = await _env.PaymentMethods.UpdateAsync(cash.Id, new SavePaymentMethodRequest { Name = "contado" });
        Assert.Equal("contado", renamed.Name);

        await Assert.ThrowsAsync<ConflictException>(
            () => _env.PaymentMethods.UpdateAsync(credit.Id, new SavePaymentMethodRequest { Name = "Contado" }));
    }

    [Fact]
    public async Task Inactive_methods_are_only_listed_on_request()
    {
        var cash = await Create("Contado");
        await Create("Crédito");

        await _env.PaymentMethods.SetStatusAsync(cash.Id, isActive: false);

        Assert.Equal(["Crédito"], (await _env.PaymentMethods.ListAsync(includeInactive: false)).Select(p => p.Name));
        Assert.Equal(2, (await _env.PaymentMethods.ListAsync(includeInactive: true)).Count);
    }

    [Fact]
    public async Task Unknown_method_returns_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _env.PaymentMethods.GetAsync(999));
    }

    private Task<PaymentMethodDto> Create(string name) =>
        _env.PaymentMethods.CreateAsync(new SavePaymentMethodRequest { Name = name });
}
