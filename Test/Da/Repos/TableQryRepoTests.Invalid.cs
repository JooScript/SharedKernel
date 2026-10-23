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
    public async Task FindAsync_WhenFilterNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.FindAsync(null!, CancellationToken.None));
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
                cancellationToken: CancellationToken.None));
    }
}
