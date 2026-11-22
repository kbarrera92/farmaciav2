using Pharmacy.Core.Common;
using Pharmacy.Core.Suppliers;
using Pharmacy.UnitTests.TestSupport;

namespace Pharmacy.UnitTests.Suppliers;

public sealed class SupplierServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public async Task Texts_are_normalized_and_empty_optionals_become_null()
    {
        var supplier = await Create(new SaveSupplierRequest
        {
            Name = "  DROGUERÍA norte ",
            TaxId = "  ",
            ContactName = "juan pérez",
            Address = "zona 1",
            Phone = " 2222-3333 ",
        });

        Assert.Equal("Droguería Norte", supplier.Name);
        Assert.Null(supplier.TaxId);
        Assert.Equal("Juan Pérez", supplier.ContactName);
        Assert.Equal("Zona 1", supplier.Address);
        Assert.Equal("2222-3333", supplier.Phone);
        Assert.True(supplier.IsActive);
    }

    [Fact]
    public async Task Name_must_be_unique_ignoring_case_and_accents_even_when_inactive()
    {
        var supplier = await Create(new SaveSupplierRequest { Name = "Droguería Norte" });
        await _env.Suppliers.SetStatusAsync(supplier.Id, isActive: false);

        await Assert.ThrowsAsync<ConflictException>(() => Create(new SaveSupplierRequest { Name = "DROGUERIA NORTE" }));
    }

    [Fact]
    public async Task Tax_id_is_unique_when_informed_but_can_be_empty_many_times()
    {
        await Create(new SaveSupplierRequest { Name = "Uno", TaxId = "123-4" });
        await Create(new SaveSupplierRequest { Name = "Dos" });
        await Create(new SaveSupplierRequest { Name = "Tres" });

        await Assert.ThrowsAsync<ConflictException>(
            () => Create(new SaveSupplierRequest { Name = "Cuatro", TaxId = " 123-4 " }));
    }

    [Fact]
    public async Task Updating_keeps_own_name_and_tax_id_but_rejects_those_of_another_supplier()
    {
        var one = await Create(new SaveSupplierRequest { Name = "Uno", TaxId = "111" });
        var two = await Create(new SaveSupplierRequest { Name = "Dos", TaxId = "222" });

        var updated = await _env.Suppliers.UpdateAsync(
            one.Id, new SaveSupplierRequest { Name = "UNO", TaxId = "111", Phone = "5555" });
        Assert.Equal("5555", updated.Phone);

        await Assert.ThrowsAsync<ConflictException>(
            () => _env.Suppliers.UpdateAsync(two.Id, new SaveSupplierRequest { Name = "Uno" }));
        await Assert.ThrowsAsync<ConflictException>(
            () => _env.Suppliers.UpdateAsync(two.Id, new SaveSupplierRequest { Name = "Dos", TaxId = "111" }));
    }

    [Fact]
    public async Task Inactive_suppliers_are_only_listed_on_request()
    {
        var one = await Create(new SaveSupplierRequest { Name = "Uno" });
        await Create(new SaveSupplierRequest { Name = "Dos" });
        await _env.Suppliers.SetStatusAsync(one.Id, isActive: false);

        Assert.Equal(["Dos"], (await _env.Suppliers.ListAsync(includeInactive: false)).Select(s => s.Name));
        Assert.Equal(2, (await _env.Suppliers.ListAsync(includeInactive: true)).Count);
    }

    [Fact]
    public async Task Unknown_supplier_returns_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _env.Suppliers.GetAsync(999));
        await Assert.ThrowsAsync<NotFoundException>(
            () => _env.Suppliers.UpdateAsync(999, new SaveSupplierRequest { Name = "X" }));
        await Assert.ThrowsAsync<NotFoundException>(() => _env.Suppliers.SetStatusAsync(999, isActive: false));
    }

    private Task<SupplierDto> Create(SaveSupplierRequest request) => _env.Suppliers.CreateAsync(request);
}
