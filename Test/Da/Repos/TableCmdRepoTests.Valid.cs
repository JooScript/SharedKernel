using SharedKernel.Da.Test.Infrastructure;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Da.Test.Repos;

public partial class TableCmdRepoTests
{
    [Fact]
    public async Task AddAsync_Single_InsertsEntity()
    {
        var entity = TestData.CreateEntity();
        entity.Id = Guid.Empty;

        var result = await _sut.AddAsync(entity, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.NotEqual(default, entity.CreatedAt);
        Assert.Equal(1, await _qryRepo.CountAsync(null, CancellationToken.None));
    }

    [Fact]
    public async Task AddAsync_Many_InsertsEntities()
    {
        var entities = new[]
        {
            TestData.CreateEntity(name: "One"),
            TestData.CreateEntity(name: "Two")
        };

        var result = await _sut.AddAsync(entities, CancellationToken.None);

        Assert.True(result);
        Assert.All(entities, e => Assert.NotEqual(Guid.Empty, e.Id));
        Assert.All(entities, e => Assert.NotEqual(default, e.CreatedAt));
        Assert.Equal(2, await _qryRepo.CountAsync(null, CancellationToken.None));
    }

    [Fact]
    public async Task AddAsync_Many_WhenEmpty_ReturnsTrue()
    {
        var result = await _sut.AddAsync(Array.Empty<TestEntity>(), CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task UpdateAsync_Single_PersistsChanges()
    {
        var existing = await SeedAsync(name: "Before");
        var originalCreatedAt = existing.CreatedAt;
        var originalCreatedBy = existing.CreatedBy;

        var update = TestData.CreateEntity(existing.Id, "After", 99);
        update.CreatedAt = DateTimeOffset.MinValue;
        update.CreatedBy = Guid.NewGuid();

        var result = await _sut.UpdateAsync(update, CancellationToken.None);

        Assert.True(result);
        _context.ChangeTracker.Clear();

        var reloaded = await _qryRepo.GetByIdAsync(existing.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("After", reloaded!.Name);
        Assert.Equal(99, reloaded.Value);
        Assert.Equal(originalCreatedAt, reloaded.CreatedAt);
        Assert.Equal(originalCreatedBy, reloaded.CreatedBy);
        Assert.NotNull(reloaded.UpdatedAt);
        Assert.NotEqual(default, update.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_Many_PersistsChanges()
    {
        var first = await SeedAsync(name: "First");
        var second = await SeedAsync(name: "Second");

        var firstUpdate = TestData.CreateEntity(first.Id, "First Updated", 10);
        var secondUpdate = TestData.CreateEntity(second.Id, "Second Updated", 20);

        var result = await _sut.UpdateAsync(new[] { firstUpdate, secondUpdate }, CancellationToken.None);

        Assert.True(result);
        _context.ChangeTracker.Clear();

        Assert.Equal("First Updated", (await _qryRepo.GetByIdAsync(first.Id))!.Name);
        Assert.Equal("Second Updated", (await _qryRepo.GetByIdAsync(second.Id))!.Name);
        Assert.NotEqual(default, firstUpdate.UpdatedAt);
        Assert.NotEqual(default, secondUpdate.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_Many_WhenEmpty_ReturnsTrue()
    {
        var result = await _sut.UpdateAsync(Array.Empty<TestEntity>(), CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task ChangeStatusAsync_Single_UpdatesStateAndAuditFields()
    {
        var entity = await SeedAsync();
        var userId = Guid.NewGuid();

        var result = await _sut.ChangeStatusAsync(
            entity.Id,
            userId,
            CurrentStateType.InActive,
            CancellationToken.None);

        Assert.True(result);
        _context.ChangeTracker.Clear();

        var reloaded = await _qryRepo.GetByIdAsync(entity.Id);
        Assert.Equal(CurrentStateType.InActive, reloaded!.CurrentState);
        Assert.Equal(userId, reloaded.UpdatedBy);
        Assert.NotNull(reloaded.UpdatedAt);
    }

    [Fact]
    public async Task ChangeStatusAsync_Many_UpdatesAll()
    {
        var first = await SeedAsync();
        var second = await SeedAsync();
        var userId = Guid.NewGuid();

        var result = await _sut.ChangeStatusAsync(
            new[] { first.Id, second.Id },
            userId,
            CurrentStateType.InActive,
            CancellationToken.None);

        Assert.True(result);
        _context.ChangeTracker.Clear();

        Assert.Equal(CurrentStateType.InActive, (await _qryRepo.GetByIdAsync(first.Id))!.CurrentState);
        Assert.Equal(CurrentStateType.InActive, (await _qryRepo.GetByIdAsync(second.Id))!.CurrentState);
    }

    [Fact]
    public async Task ChangeStatusAsync_Many_WhenEmpty_ReturnsTrue()
    {
        var result = await _sut.ChangeStatusAsync(
            Array.Empty<Guid>(),
            Guid.NewGuid(),
            CurrentStateType.InActive,
            CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task ActivateAsync_SetsActiveState()
    {
        var entity = await SeedAsync(state: CurrentStateType.InActive);

        var result = await _sut.ActivateAsync(entity.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result);
        _context.ChangeTracker.Clear();
        Assert.Equal(CurrentStateType.Active, (await _qryRepo.GetByIdAsync(entity.Id))!.CurrentState);
    }

    [Fact]
    public async Task DeactivateAsync_SetsInActiveState()
    {
        var entity = await SeedAsync();

        var result = await _sut.DeactivateAsync(entity.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result);
        _context.ChangeTracker.Clear();
        Assert.Equal(CurrentStateType.InActive, (await _qryRepo.GetByIdAsync(entity.Id))!.CurrentState);
    }

    [Fact]
    public async Task SoftDeleteAsync_Single_SetsDeletedState()
    {
        var entity = await SeedAsync();

        var result = await _sut.SoftDeleteAsync(entity.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result);
        _context.ChangeTracker.Clear();
        Assert.Equal(CurrentStateType.Deleted, (await _qryRepo.GetByIdAsync(entity.Id))!.CurrentState);
    }

    [Fact]
    public async Task SoftDeleteAsync_Many_SetsDeletedState()
    {
        var first = await SeedAsync();
        var second = await SeedAsync();

        var result = await _sut.SoftDeleteAsync(
            new[] { first.Id, second.Id },
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result);
        _context.ChangeTracker.Clear();
        Assert.Equal(CurrentStateType.Deleted, (await _qryRepo.GetByIdAsync(first.Id))!.CurrentState);
        Assert.Equal(CurrentStateType.Deleted, (await _qryRepo.GetByIdAsync(second.Id))!.CurrentState);
    }

    [Fact]
    public async Task HardDeleteAsync_Single_RemovesEntity()
    {
        var entity = await SeedAsync();

        var result = await _sut.HardDeleteAsync(entity.Id, CancellationToken.None);

        Assert.True(result);
        _context.ChangeTracker.Clear();
        Assert.Null(await _qryRepo.GetByIdAsync(entity.Id));
    }

    [Fact]
    public async Task HardDeleteAsync_Many_RemovesEntities()
    {
        var first = await SeedAsync();
        var second = await SeedAsync();

        var result = await _sut.HardDeleteAsync(
            new[] { first.Id, second.Id },
            CancellationToken.None);

        Assert.True(result);
        _context.ChangeTracker.Clear();
        Assert.Equal(0, await _qryRepo.CountAsync(null, CancellationToken.None));
    }

    [Fact]
    public async Task HardDeleteAsync_Many_WhenEmpty_ReturnsTrue()
    {
        var result = await _sut.HardDeleteAsync(Array.Empty<Guid>(), CancellationToken.None);

        Assert.True(result);
    }
}
