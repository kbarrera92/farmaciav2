using Microsoft.EntityFrameworkCore;
using Pharmacy.Core.Common;

namespace Pharmacy.Core.Categories;

public sealed class CategoryService(IAppDbContext db)
{
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(bool includeInactive, CancellationToken ct = default)
    {
        var query = db.Categories.AsNoTracking();
        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        return await Project(query.OrderBy(c => c.Name)).ToListAsync(ct);
    }

    public async Task<CategoryDto> GetAsync(int id, CancellationToken ct = default) =>
        await Project(db.Categories.AsNoTracking().Where(c => c.Id == id)).FirstOrDefaultAsync(ct)
        ?? throw NotFound();

    public async Task<CategoryDto> CreateAsync(SaveCategoryRequest request, CancellationToken ct = default)
    {
        var name = TextNormalizer.ToSentenceCase(request.Name);
        await EnsureUniqueNameAsync(name, excludeId: null, ct);

        var category = new Category { Name = name };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return await GetAsync(category.Id, ct);
    }

    public async Task<CategoryDto> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken ct = default)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw NotFound();
        var name = TextNormalizer.ToSentenceCase(request.Name);
        await EnsureUniqueNameAsync(name, excludeId: id, ct);

        category.Name = name;
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task<CategoryDto> SetStatusAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw NotFound();
        category.IsActive = isActive;
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    private static IQueryable<CategoryDto> Project(IQueryable<Category> query) =>
        query.Select(c => new CategoryDto(
            c.Id, c.Name, c.IsActive,
            new AuditInfo(c.CreatedAtUtc, c.CreatedBy!.FullName, c.UpdatedAtUtc, c.UpdatedBy!.FullName)));

    /// <summary>
    /// The Name column uses a case and accent insensitive collation, so a plain equality
    /// already treats "Analgésicos" and "ANALGESICOS" as the same name.
    /// </summary>
    private async Task EnsureUniqueNameAsync(string name, int? excludeId, CancellationToken ct)
    {
        var exists = await db.Categories.AnyAsync(
            c => c.Name == name && (excludeId == null || c.Id != excludeId), ct);
        if (exists)
            throw new ConflictException($"Ya existe una categoría con el nombre '{name}'.");
    }

    private static NotFoundException NotFound() => new("La categoría no existe.");
}
