using SharedKernel.Da.Test.Infrastructure;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Da.Test.Repos;

public partial class TableQryRepoTests
{
    [Fact]
    public async Task GetByIdAsync_WhenEntityExists_ReturnsEntity()
    {
        var entity = await SeedAsync(name: "First");

        var result = await _sut.GetByIdAsync(entity.Id, e => e, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result!.Id);
        Assert.Equal("First", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityMissing_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), e => e, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithSelector_ReturnsProjection()
    {
        var entity = await SeedAsync(name: "Projected");

        var result = await _sut.GetByIdAsync(entity.Id, e => e.Name, CancellationToken.None);

        Assert.Equal("Projected", result);
    }

    [Fact]
    public async Task GetByIdAsync_WithSelector_WhenMissing_ReturnsDefault()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), e => e.Name, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdsAsync_ReturnsMatchingEntities()
    {
        var first = await SeedAsync();
        var second = await SeedAsync();
        await SeedAsync();

        var result = (await _sut.GetByIdsAsync(new[] { first.Id, second.Id }, CancellationToken.None)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Id == first.Id);
        Assert.Contains(result, x => x.Id == second.Id);
    }

    [Fact]
    public async Task GetByIdsAsync_WhenEmpty_ReturnsEmpty()
    {
        await SeedAsync();

        var result = await _sut.GetByIdsAsync(Array.Empty<Guid>(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task FindAsync_ReturnsMatchingEntities()
    {
        var entity = await SeedAsync(value: 42);

        var result = (await _sut.FindAsync(e => e.Value == 42, CancellationToken.None)).ToList();

        Assert.Single(result);
        Assert.Equal(entity.Id, result[0].Id);
    }

    [Fact]
    public async Task FindAsync_WithProjection_ReturnsMatchingValues()
    {
        await SeedAsync(value: 7);
        await SeedAsync(value: 10);

        var result = (await _sut.FindAsync(
            e => e.Value > 5,
            e => e.Value,
            e => e.Value,
            false,
            CancellationToken.None)).ToList();

        Assert.Equal(new[] { 7, 10 }, result);
    }

    [Fact]
    public async Task GetFirstOrDefaultAsync_ReturnsFirstMatch()
    {
        await SeedAsync(value: 1);
        var target = await SeedAsync(value: 99);

        var result = await _sut.GetFirstOrDefaultAsync(e => e.Value == 99, e => e, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(target.Id, result!.Id);
    }

    [Fact]
    public async Task GetFirstOrDefaultAsync_WhenMissing_ReturnsNull()
    {
        var result = await _sut.GetFirstOrDefaultAsync(e => e.Value == 12345, e => e, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTrackedOrLoadAsync_WhenAlreadyTracked_ReturnsSameInstance()
    {
        var entity = await SeedAsync();

        var result = await _sut.GetTrackedOrLoadAsync(entity.Id, CancellationToken.None);

        Assert.Same(entity, result);
    }

    [Fact]
    public async Task GetTrackedOrLoadAsync_WhenNotTracked_LoadsFromStore()
    {
        var entity = await SeedAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetTrackedOrLoadAsync(entity.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result!.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var active = await SeedAsync();
        var inactive = await SeedAsync(state: CurrentStateType.InActive);
        var deleted = await SeedAsync(state: CurrentStateType.Deleted);

        var result = (await _sut.GetAllAsync(CancellationToken.None)).ToList();

        Assert.Equal(3, result.Count);
        Assert.Contains(result, x => x.Id == active.Id);
        Assert.Contains(result, x => x.Id == inactive.Id);
        Assert.Contains(result, x => x.Id == deleted.Id);
    }

    [Fact]
    public async Task CountAsync_WithoutFilter_ReturnsTotal()
    {
        await SeedAsync();
        await SeedAsync();

        var result = await _sut.CountAsync(null, CancellationToken.None);

        Assert.Equal(2, result);
    }

    [Fact]
    public async Task CountAsync_WithFilter_ReturnsFilteredCount()
    {
        var target = await SeedAsync(value: 5);
        await SeedAsync(value: 10);

        var result = await _sut.CountAsync(e => e.Value == target.Value, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task IsExistsAsync_WithFilter_ReturnsTrueWhenMatch()
    {
        var entity = await SeedAsync();

        var result = await _sut.IsExistsAsync(e => e.Id == entity.Id, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task IsExistsAsync_WithId_ReturnsTrueWhenMatch()
    {
        var entity = await SeedAsync();

        var result = await _sut.IsExistsAsync(entity.Id, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task IsExistsAsync_WhenMissing_ReturnsFalse()
    {
        var result = await _sut.IsExistsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task FindAsync_WithFilterAndSelector_ReturnsProjection()
    {
        await SeedAsync(name: "A", value: 1);
        await SeedAsync(name: "B", value: 2);
        await SeedAsync(name: "C", value: 3);

        var result = (await _sut.FindAsync(
            filter: e => e.Value >= 2,
            selector: e => e.Name,
            orderBy: e => e.Value,
            isDescending: false,
            cancellationToken: CancellationToken.None)).ToList();

        Assert.Equal(new[] { "B", "C" }, result);
    }

    [Fact]
    public async Task FindAsync_WithoutSelector_ReturnsEntities()
    {
        await SeedAsync();
        await SeedAsync();

        var result = (await _sut.FindAsync<TestEntity>(
            filter: null,
            selector: null,
            orderBy: null,
            isDescending: false,
            cancellationToken: CancellationToken.None)).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetPagedListAsync_ReturnsRequestedPage()
    {
        for (var i = 0; i < 5; i++)
            await SeedAsync(value: i);

        var result = await _sut.GetPagedListAsync<TestEntity>(
            pageNumber: 2,
            pageSize: 2,
            filter: null,
            selector: null,
            orderBy: null,
            isDescending: false,
            cancellationToken: CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.CurrentPage);
        Assert.Equal(2, result.PageSize);
        Assert.True(result.HasPrevious);
        Assert.True(result.HasNext);
    }

    [Fact]
    public async Task GetPagedListAsync_WithFilter_ReturnsFilteredCount()
    {
        var target = await SeedAsync(value: 50);
        for (var i = 0; i < 3; i++)
            await SeedAsync(value: i);

        var result = await _sut.GetPagedListAsync<TestEntity>(
            pageNumber: 1,
            pageSize: 10,
            filter: e => e.Value == target.Value,
            selector: null,
            orderBy: null,
            isDescending: false,
            cancellationToken: CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetPagedListAsync_WithOrdering_Descending_OrdersItems()
    {
        await SeedAsync(value: 1);
        await SeedAsync(value: 3);
        await SeedAsync(value: 2);

        var result = await _sut.GetPagedListAsync<TestEntity>(
            pageNumber: 1,
            pageSize: 10,
            filter: null,
            selector: null,
            orderBy: e => e.Value,
            isDescending: true,
            cancellationToken: CancellationToken.None);

        Assert.Equal(new[] { 3, 2, 1 }, result.Items.Select(e => e.Value));
    }

    [Fact]
    public async Task GetPagedListAsync_WithSelector_ReturnsProjection()
    {
        await SeedAsync(value: 1);
        await SeedAsync(value: 2);

        var result = await _sut.GetPagedListAsync<int>(
            pageNumber: 1,
            pageSize: 10,
            filter: null,
            selector: e => e.Value,
            orderBy: e => e.Value,
            isDescending: false,
            cancellationToken: CancellationToken.None);

        Assert.Equal(new[] { 1, 2 }, result.Items);
    }
}
