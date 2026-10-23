using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Da.Repos;
using SharedKernel.Da.Test.Infrastructure;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Da.Test.Repos;

public partial class TableQryRepoTests : IAsyncDisposable
{
    private readonly SharedKernelDaTestDbContext _context;
    private readonly SharedKernelTableQryRepo<TestEntity, SharedKernelDaTestDbContext> _sut;

    public TableQryRepoTests()
    {
        _context = TestDbContextFactory.Create();
        _sut = new SharedKernelTableQryRepo<TestEntity, SharedKernelDaTestDbContext>(
            _context,
            NullLogger<SharedKernelTableQryRepo<TestEntity, SharedKernelDaTestDbContext>>.Instance);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private async Task<TestEntity> SeedAsync(
        Guid? id = null,
        string name = "Entity",
        int value = 1,
        CurrentStateType state = CurrentStateType.Active)
    {
        var entity = TestData.CreateEntity(id, name, value, state);
        await TestData.SeedAsync(_context, entity);
        return entity;
    }
}
