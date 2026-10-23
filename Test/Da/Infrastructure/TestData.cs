using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Da.Test.Infrastructure;

public static class TestData
{
    public static TestEntity CreateEntity(
        Guid? id = null,
        string name = "Entity",
        int value = 1,
        CurrentStateType state = CurrentStateType.Active)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Value = value,
            CurrentState = state,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = Guid.NewGuid()
        };

    public static TestView CreateView(
        Guid? id = null,
        string name = "View",
        int value = 1)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Value = value
        };

    public static async Task SeedAsync(
        SharedKernelDaTestDbContext context,
        params object[] entities)
    {
        foreach (var entity in entities)
            context.Add(entity);

        await context.SaveChangesAsync();
    }
}
