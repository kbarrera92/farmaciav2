using Pharmacy.Core.Categories;
using Pharmacy.Core.Common;
using Pharmacy.UnitTests.TestSupport;

namespace Pharmacy.UnitTests.Categories;

public sealed class CategoryServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public async Task Name_is_trimmed_and_must_be_unique_ignoring_case_accents_and_spaces()
    {
        var created = await Create("  Analgésicos ");
        Assert.Equal("Analgésicos", created.Name);
        Assert.True(created.IsActive);

        await Assert.ThrowsAsync<ConflictException>(() => Create(" ANALGESICOS "));
        await Assert.ThrowsAsync<ConflictException>(() => Create("analgésicos"));
    }

    [Fact]
    public async Task Enie_is_a_different_letter_not_an_accent()
    {
        await Create("Baños");

        var other = await Create("Banos");

        Assert.Equal("Banos", other.Name);
    }

    [Fact]
    public async Task Renaming_keeping_the_same_name_is_allowed_but_not_to_another_existing_name()
    {
        var analgesics = await Create("Analgésicos");
        var antibiotics = await Create("Antibióticos");

        var renamed = await _env.Categories.UpdateAsync(analgesics.Id, new SaveCategoryRequest { Name = "ANALGESICOS" });
        Assert.Equal("Analgesicos", renamed.Name);

        await Assert.ThrowsAsync<ConflictException>(
            () => _env.Categories.UpdateAsync(antibiotics.Id, new SaveCategoryRequest { Name = "ANALGÉSICOS" }));
    }

    [Fact]
    public async Task Inactive_names_still_count_for_uniqueness()
    {
        var vitamins = await Create("Vitaminas");
        await _env.Categories.SetStatusAsync(vitamins.Id, isActive: false);

        await Assert.ThrowsAsync<ConflictException>(() => Create("Vitaminas"));
    }

    [Fact]
    public async Task Inactive_categories_are_only_listed_on_request()
    {
        var vitamins = await Create("Vitaminas");
        await Create("Antigripales");

        await _env.Categories.SetStatusAsync(vitamins.Id, isActive: false);

        Assert.Equal(["Antigripales"], (await _env.Categories.ListAsync(includeInactive: false)).Select(c => c.Name));
        Assert.Equal(2, (await _env.Categories.ListAsync(includeInactive: true)).Count);
    }

    [Fact]
    public async Task Unknown_category_returns_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _env.Categories.GetAsync(999));
        await Assert.ThrowsAsync<NotFoundException>(
            () => _env.Categories.UpdateAsync(999, new SaveCategoryRequest { Name = "X" }));
        await Assert.ThrowsAsync<NotFoundException>(() => _env.Categories.SetStatusAsync(999, isActive: false));
    }

    private Task<CategoryDto> Create(string name) =>
        _env.Categories.CreateAsync(new SaveCategoryRequest { Name = name });
}
