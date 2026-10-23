using Microsoft.EntityFrameworkCore;
using SharedKernel.Da.Repos;
using SharedKernel.Da.Test.Infrastructure;

namespace SharedKernel.Da.Test.Repos;

public class UnitOfWorkTests
{
    [Fact]
    public async Task SaveChangesAsync_PersistsAddedEntity()
    {
        await using var context = TestDbContextFactory.Create();
        await using var sut = new SharedKernelUnitOfWork<SharedKernelDaTestDbContext>(context);
        var entity = TestData.CreateEntity();
        context.Add(entity);

        var result = await sut.SaveChangesAsync();

        Assert.Equal(1, result);
        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public async Task CommitAsync_PersistsChanges()
    {
        await using var context = TestDbContextFactory.Create();
        await using var sut = new SharedKernelUnitOfWork<SharedKernelDaTestDbContext>(context);
        var entity = TestData.CreateEntity();
        context.Add(entity);

        await sut.CommitAsync();

        context.ChangeTracker.Clear();
        Assert.NotNull(await context.Entities.FindAsync(entity.Id));
    }

    [Fact]
    public async Task BeginTransactionAsync_ThenCommit_PersistsChanges()
    {
        await using var context = TestDbContextFactory.Create();
        await using var sut = new SharedKernelUnitOfWork<SharedKernelDaTestDbContext>(context);
        context.Add(TestData.CreateEntity());

        await sut.BeginTransactionAsync();
        await sut.CommitAsync();

        Assert.Equal(1, await context.Entities.CountAsync());
    }

    [Fact]
    public async Task RollbackAsync_AfterBegin_CompletesWithoutError()
    {
        await using var context = TestDbContextFactory.Create();
        await using var sut = new SharedKernelUnitOfWork<SharedKernelDaTestDbContext>(context);

        await sut.BeginTransactionAsync();
        await sut.RollbackAsync();

        Assert.True(true);
    }

    [Fact]
    public async Task DisposeAsync_DisposesContext()
    {
        var context = TestDbContextFactory.Create();
        var sut = new SharedKernelUnitOfWork<SharedKernelDaTestDbContext>(context);

        await sut.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => context.SaveChangesAsync());
    }
}
