using SharedKernel.Da.Test.Infrastructure;

namespace SharedKernel.Da.Test.Repos;

public partial class TableQryRepoTests
{
    [Fact]
    public async Task GetByIdsAsync_WhenNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.GetByIdsAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task FindAsync_WhenFilterNull_ReturnsAllEntities()
    {
        await SeedAsync();
        await SeedAsync();

        var result = await _sut.FindAsync(null, CancellationToken.None);

        Assert.Equal(2, result.Count());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPagedListAsync_WhenPageNumberInvalid_ThrowsArgumentOutOfRange(int pageNumber)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _sut.GetPagedListAsync<TestEntity>(
                pageNumber,
                pageSize: 10,
                filter: null,
                selector: null,
                orderBy: null,
                isDescending: false,
                cancellationToken: CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetPagedListAsync_WhenPageSizeInvalid_ThrowsArgumentOutOfRange(int pageSize)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _sut.GetPagedListAsync<TestEntity>(
                pageNumber: 1,
                pageSize,
                filter: null,
                selector: null,
                orderBy: null,
                isDescending: false,
                cancellationToken: CancellationToken.None));
    }
}
