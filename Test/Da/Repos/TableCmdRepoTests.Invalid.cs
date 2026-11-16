using SharedKernel.Da.Test.Infrastructure;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Da.Test.Repos;

public partial class TableCmdRepoTests
{
    [Fact]
    public async Task AddAsync_Single_WhenNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.AddAsync((TestEntity)null!, CancellationToken.None));
    }

    [Fact]
    public async Task AddAsync_Many_WhenNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.AddAsync((IEnumerable<TestEntity>)null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_Single_WhenNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.UpdateAsync((TestEntity)null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_Single_WhenMissing_ReturnsFalse()
    {
        var entity = TestData.CreateEntity();

        var result = await _sut.UpdateAsync(entity, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_Many_WhenNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.UpdateAsync((IEnumerable<TestEntity>)null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_Many_WhenEntityMissing_ReturnsFalse()
    {
        var existing = await SeedAsync();
        var missing = TestData.CreateEntity();

        var result = await _sut.UpdateAsync(new[] { existing, missing }, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ChangeStatusAsync_Single_WhenMissing_ReturnsFalse()
    {
        var result = await _sut.ChangeStatusAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CurrentStateType.InActive,
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ChangeStatusAsync_Many_WhenSomeMissing_ReturnsFalse()
    {
        var existing = await SeedAsync();

        var result = await _sut.ChangeStatusAsync(
            new[] { existing.Id, Guid.NewGuid() },
            Guid.NewGuid(),
            CurrentStateType.InActive,
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task HardDeleteAsync_Single_WhenMissing_ReturnsFalse()
    {
        var result = await _sut.HardDeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task HardDeleteAsync_Many_WhenSomeMissing_ReturnsFalse()
    {
        var existing = await SeedAsync();

        var result = await _sut.HardDeleteAsync(
            new[] { existing.Id, Guid.NewGuid() },
            CancellationToken.None);

        Assert.False(result);
    }
}
