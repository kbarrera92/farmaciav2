using Pharmacy.Core.Branches;
using Pharmacy.Core.Common;
using Pharmacy.UnitTests.TestSupport;

namespace Pharmacy.UnitTests.Branches;

public sealed class BranchServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public async Task Name_must_be_unique_ignoring_case_accents_and_spaces()
    {
        await Create("Jalapa Norte");

        await Assert.ThrowsAsync<ConflictException>(() => Create(" JALAPA NORTE "));
        await Assert.ThrowsAsync<ConflictException>(() => Create("Jalapá Norte"));
        // "Central" is seeded by the test environment.
        await Assert.ThrowsAsync<ConflictException>(() => Create("céntral"));
    }

    [Fact]
    public async Task Renaming_to_another_existing_name_is_rejected()
    {
        var north = await Create("Jalapa Norte");

        await Assert.ThrowsAsync<ConflictException>(
            () => _env.Branches.UpdateAsync(north.Id, new SaveBranchRequest { Name = "CENTRAL", Address = "Zona 2" }));
    }

    private Task<BranchDto> Create(string name) =>
        _env.Branches.CreateAsync(new SaveBranchRequest { Name = name, Address = "Zona 2" });
}
