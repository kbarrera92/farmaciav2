using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.Suppliers;

public sealed class SupplierService(IAppDbContext db)
{
    public async Task<IReadOnlyList<SupplierDto>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        var query = db.Suppliers.AsNoTracking();
        if (!includeInactive)
            query = query.Where(s => s.IsActive);

        return await Project(query.OrderBy(s => s.Name)).ToListAsync(ct);
    }

    public async Task<SupplierDto> GetAsync(int id, CancellationToken ct = default) =>
        await Project(db.Suppliers.AsNoTracking().Where(s => s.Id == id)).FirstOrDefaultAsync(ct)
        ?? throw NotFound();

    public async Task<SupplierDto> CreateAsync(SaveSupplierRequest request, CancellationToken ct = default)
    {
        var supplier = new Supplier();
        await ApplyAsync(supplier, request, ct);
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(ct);

        return await GetAsync(supplier.Id, ct);
    }

    public async Task<SupplierDto> UpdateAsync(int id, SaveSupplierRequest request, CancellationToken ct = default)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct) ?? throw NotFound();
        await ApplyAsync(supplier, request, ct);
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task<SupplierDto> SetStatusAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct) ?? throw NotFound();
        supplier.IsActive = isActive;
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    private async Task ApplyAsync(Supplier supplier, SaveSupplierRequest request, CancellationToken ct)
    {
        var name = TextNormalizer.ToTitleCase(request.Name);
        var taxId = TextNormalizer.TrimToNull(request.TaxId);
        await EnsureUniqueAsync(name, taxId, excludeId: supplier.Id == 0 ? null : supplier.Id, ct);

        supplier.Name = name;
        supplier.TaxId = taxId;
        supplier.Address = TextNormalizer.CapitalizeFirstOrNull(request.Address);
        supplier.Phone = TextNormalizer.TrimToNull(request.Phone);
        supplier.Email = TextNormalizer.TrimToNull(request.Email);
        supplier.ContactName = TextNormalizer.ToTitleCaseOrNull(request.ContactName);
        supplier.ContactPhone = TextNormalizer.TrimToNull(request.ContactPhone);
    }

    private static IQueryable<SupplierDto> Project(IQueryable<Supplier> query) =>
        query.Select(s => new SupplierDto(
            s.Id, s.Name, s.TaxId, s.Address, s.Phone, s.Email, s.ContactName, s.ContactPhone, s.IsActive,
            new AuditInfo(s.CreatedAtUtc, s.CreatedBy!.FullName, s.UpdatedAtUtc, s.UpdatedBy!.FullName)));

    /// <summary>
    /// The Name column uses a case and accent insensitive collation, so plain equality
    /// treats "Droguería Norte" and "DROGUERIA NORTE" as the same name. The tax id is unique when informed.
    /// </summary>
    private async Task EnsureUniqueAsync(string name, string? taxId, int? excludeId, CancellationToken ct)
    {
        if (await db.Suppliers.AnyAsync(s => s.Name == name && (excludeId == null || s.Id != excludeId), ct))
            throw new ConflictException($"Ya existe un proveedor con el nombre '{name}'.");

        if (taxId is not null
            && await db.Suppliers.AnyAsync(s => s.TaxId == taxId && (excludeId == null || s.Id != excludeId), ct))
            throw new ConflictException($"Ya existe un proveedor con el NIT '{taxId}'.");
    }

    private static NotFoundException NotFound() => new("El proveedor no existe.");
}
