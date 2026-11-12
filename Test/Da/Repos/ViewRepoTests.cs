using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Da.Repos;
using SharedKernel.Da.Test.Infrastructure;

namespace SharedKernel.Da.Test.Repos;

public class ViewRepoTests : IAsyncDisposable
{
    private readonly SharedKernelDaTestDbContext _context;
    private readonly SharedKernelViewRepo<TestView, SharedKernelDaTestDbContext> _sut;

    public ViewRepoTests()
    {
        _context = TestDbContextFactory.Create();
        _sut = new SharedKernelViewRepo<TestView, SharedKernelDaTestDbContext>(
            _context,
            NullLogger<SharedKernelViewRepo<TestView, SharedKernelDaTestDbContext>>.Instance);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private async Task<TestView> SeedAsync(Guid? id = null, string name = "View", int value = 1)
    {
        var view = TestData.CreateView(id, name, value);
        await TestData.SeedAsync(_context, view);
        return view;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllViews()
    {
        await SeedAsync();
        await SeedAsync();

        var result = await _sut.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetFirstOrDefaultAsync_WhenExists_ReturnsView()
    {
        var view = await SeedAsync(name: "Target");

        var result = await _sut.GetFirstOrDefaultAsync(v => v.Id == view.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Target", result!.Name);
    }

    [Fact]
    public async Task GetFirstOrDefaultAsync_WhenIdMissing_ReturnsNull()
    {
        var result = await _sut.GetFirstOrDefaultAsync(v => v.Id == Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFirstOrDefaultAsync_ReturnsFirstMatch()
    {
        await SeedAsync(value: 1);
        var target = await SeedAsync(value: 10);

        var result = await _sut.GetFirstOrDefaultAsync(v => v.Value == 10, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(target.Id, result!.Id);
    }

    [Fact]
    public async Task GetFirstOrDefaultAsync_WhenMissing_ReturnsNull()
    {
        var result = await _sut.GetFirstOrDefaultAsync(v => v.Value == 404, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFirstOrDefaultAsync_WhenFilterNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.GetFirstOrDefaultAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task FindAsync_ReturnsFilteredViews()
    {
        await SeedAsync(value: 1);
        await SeedAsync(value: 2);
        await SeedAsync(value: 3);

        var result = await _sut.FindAsync(v => v.Value >= 2, CancellationToken.None);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task FindAsync_WhenFilterNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.FindAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task FindAsync_WithNameFilter_ReturnsFilteredViews()
    {
        var target = await SeedAsync(name: "FindMe");
        await SeedAsync(name: "Other");

        var result = await _sut.FindAsync(v => v.Name == target.Name, CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task CountAsync_WithoutFilter_ReturnsTotal()
    {
        await SeedAsync(value: 1);
        await SeedAsync(value: 1);
        await SeedAsync(value: 2);

        var result = await _sut.CountAsync(null, CancellationToken.None);

        Assert.Equal(3, result);
    }

    [Fact]
    public async Task CountAsync_WithFilter_ReturnsFilteredCount()
    {
        await SeedAsync(value: 1);
        await SeedAsync(value: 1);
        await SeedAsync(value: 2);

        var result = await _sut.CountAsync(v => v.Value == 1, CancellationToken.None);

        Assert.Equal(2, result);
    }

    [Fact]
    public async Task IsExistsAsync_ReturnsTrueWhenMatch()
    {
        var view = await SeedAsync();

        var result = await _sut.IsExistsAsync(v => v.Id == view.Id, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task IsExistsAsync_WhenMissing_ReturnsFalse()
    {
        var result = await _sut.IsExistsAsync(v => v.Value == 404, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task IsExistsAsync_WhenFilterNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.IsExistsAsync(null!, CancellationToken.None));
    }
}
