using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Auth;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.Branches;

public sealed class BranchService(IAppDbContext db)
{
    public async Task<IReadOnlyList<BranchDto>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        var query = db.Branches.AsNoTracking();
        if (!includeInactive)
            query = query.Where(b => b.IsActive);

        return await query
            .OrderBy(b => b.Name)
            .Select(b => new BranchDto(
                b.Id, b.Name, b.Address, b.Phone, b.IsActive,
                db.Users.Count(u => u.BranchId == b.Id && u.IsActive),
                new AuditInfo(b.CreatedAtUtc, b.CreatedBy!.FullName, b.UpdatedAtUtc, b.UpdatedBy!.FullName)))
            .ToListAsync(ct);
    }

    /// <summary>Active branches as id/name pairs, for selectors in other modules.</summary>
    public async Task<IReadOnlyList<NamedReference>> ListActiveLookupAsync(CancellationToken ct = default) =>
        await db.Branches.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new NamedReference(b.Id, b.Name))
            .ToListAsync(ct);

    public async Task<BranchDto> GetAsync(int id, CancellationToken ct = default)
    {
        return await db.Branches.AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BranchDto(
                b.Id, b.Name, b.Address, b.Phone, b.IsActive,
                db.Users.Count(u => u.BranchId == b.Id && u.IsActive),
                new AuditInfo(b.CreatedAtUtc, b.CreatedBy!.FullName, b.UpdatedAtUtc, b.UpdatedBy!.FullName)))
            .FirstOrDefaultAsync(ct)
            ?? throw NotFound();
    }

    public async Task<BranchDto> CreateAsync(SaveBranchRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        await EnsureUniqueNameAsync(name, excludeId: null, ct);

        var branch = new Branch
        {
            Name = name,
            Address = request.Address.Trim(),
            Phone = TextNormalizer.TrimToNull(request.Phone),
        };
        db.Branches.Add(branch);
        await db.SaveChangesAsync(ct);

        return await GetAsync(branch.Id, ct);
    }

    public async Task<BranchDto> UpdateAsync(int id, SaveBranchRequest request, CancellationToken ct = default)
    {
        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == id, ct) ?? throw NotFound();
        var name = request.Name.Trim();
        await EnsureUniqueNameAsync(name, excludeId: id, ct);

        branch.Name = name;
        branch.Address = request.Address.Trim();
        branch.Phone = TextNormalizer.TrimToNull(request.Phone);
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task<BranchDto> SetStatusAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == id, ct) ?? throw NotFound();

        if (!isActive && await db.Users.AnyAsync(u => u.BranchId == id && u.IsActive, ct))
            throw new BusinessRuleException(
                "No se puede desactivar una sucursal con usuarios activos. Reasigne o desactive esos usuarios primero.");

        branch.IsActive = isActive;
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    /// <summary>The Name column uses a case and accent insensitive collation, so plain equality is enough.</summary>
    private async Task EnsureUniqueNameAsync(string name, int? excludeId, CancellationToken ct)
    {
        var exists = await db.Branches.AnyAsync(
            b => b.Name == name && (excludeId == null || b.Id != excludeId), ct);
        if (exists)
            throw new ConflictException($"Ya existe una sucursal con el nombre '{name}'.");
    }

    private static NotFoundException NotFound() => new("La sucursal no existe.");
}
