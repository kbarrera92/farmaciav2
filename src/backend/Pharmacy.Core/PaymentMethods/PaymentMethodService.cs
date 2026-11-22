using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.PaymentMethods;

public sealed class PaymentMethodService(IAppDbContext db)
{
    public async Task<IReadOnlyList<PaymentMethodDto>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        var query = db.PaymentMethods.AsNoTracking();
        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        return await Project(query.OrderBy(p => p.Name)).ToListAsync(ct);
    }

    public async Task<PaymentMethodDto> GetAsync(int id, CancellationToken ct = default) =>
        await Project(db.PaymentMethods.AsNoTracking().Where(p => p.Id == id)).FirstOrDefaultAsync(ct)
        ?? throw NotFound();

    public async Task<PaymentMethodDto> CreateAsync(SavePaymentMethodRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        await EnsureUniqueNameAsync(name, excludeId: null, ct);

        var paymentMethod = new PaymentMethod { Name = name };
        db.PaymentMethods.Add(paymentMethod);
        await db.SaveChangesAsync(ct);

        return await GetAsync(paymentMethod.Id, ct);
    }

    public async Task<PaymentMethodDto> UpdateAsync(int id, SavePaymentMethodRequest request, CancellationToken ct = default)
    {
        var paymentMethod = await db.PaymentMethods.FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw NotFound();
        var name = request.Name.Trim();
        await EnsureUniqueNameAsync(name, excludeId: id, ct);

        paymentMethod.Name = name;
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task<PaymentMethodDto> SetStatusAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var paymentMethod = await db.PaymentMethods.FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw NotFound();
        paymentMethod.IsActive = isActive;
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    private static IQueryable<PaymentMethodDto> Project(IQueryable<PaymentMethod> query) =>
        query.Select(p => new PaymentMethodDto(
            p.Id, p.Name, p.IsActive,
            new AuditInfo(p.CreatedAtUtc, p.CreatedBy!.FullName, p.UpdatedAtUtc, p.UpdatedBy!.FullName)));

    private async Task EnsureUniqueNameAsync(string name, int? excludeId, CancellationToken ct)
    {
        var normalized = TextNormalizer.Normalize(name);
        var exists = await db.PaymentMethods.AnyAsync(
            p => p.Name.ToUpper() == normalized && (excludeId == null || p.Id != excludeId), ct);
        if (exists)
            throw new ConflictException($"Ya existe una forma de pago con el nombre '{name}'.");
    }

    private static NotFoundException NotFound() => new("La forma de pago no existe.");
}
