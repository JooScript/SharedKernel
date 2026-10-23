using SharedKernel.Bl.Services.Base;
using SharedKernel.Bl.Test.Infrastructure;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Bl.Test.Services;

public class BlHelperTests
{
    [Fact]
    public void NormalizeDbObjToAdd_ResetsIdentityAndSetsAuditFields()
    {
        var userId = Guid.NewGuid();
        var entity = new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Entity",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedBy = Guid.NewGuid(),
            CurrentState = CurrentStateType.Deleted
        };

        var result = BlHelper.NormalizeDbObjToAdd(entity, userId);

        Assert.Same(entity, result);
        Assert.Equal(Guid.Empty, result.Id);
        Assert.Equal(userId, result.CreatedBy);
        Assert.Equal(CurrentStateType.Active, result.CurrentState);
        Assert.True(result.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void NormalizeDbObjsToAdd_NormalizesAllItems()
    {
        var userId = Guid.NewGuid();
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), Name = "First" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Second" }
        };

        var result = BlHelper.NormalizeDbObjsToAdd(entities, userId).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(Guid.Empty, e.Id));
        Assert.All(result, e => Assert.Equal(userId, e.CreatedBy));
        Assert.All(result, e => Assert.Equal(CurrentStateType.Active, e.CurrentState));
    }

    [Fact]
    public void NormalizeDbObjToUpdate_SetsAuditFields()
    {
        var userId = Guid.NewGuid();
        var entity = new TestEntity { Name = "Entity" };

        var result = BlHelper.NormalizeDbObjToUpdate(entity, userId);

        Assert.Same(entity, result);
        Assert.Equal(userId, result.UpdatedBy);
        Assert.NotNull(result.UpdatedAt);
        Assert.True(result.UpdatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }
}
