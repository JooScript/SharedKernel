using SharedKernel.Bl.Events.Args;
using SharedKernel.Bl.Events.Publishers;
using SharedKernel.Bl.Test.Infrastructure;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Bl.Test.Events;

public class CrudPublisherTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Publish_Created_RaisesCreatedEvent()
    {
        var publisher = new CrudPublisher();
        var entity = new TestEntity { Id = Guid.NewGuid() };
        EntityCreatedEventArgs? received = null;
        publisher.Created += (_, args) => received = args;

        publisher.Publish(new EntityCreatedEventArgs(entity, UserId));

        Assert.NotNull(received);
        Assert.Same(entity, received!.Entity);
        Assert.Equal(UserId, received.UserId);
        Assert.Equal(SharedKernel.Bl.Enums.Events.EntityEnums.enCrudOp.Create, received.Operation);
    }

    [Fact]
    public void Publish_Updated_RaisesUpdatedEvent()
    {
        var publisher = new CrudPublisher();
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Current" };
        var original = new TestEntity { Id = entity.Id, Name = "Original" };
        EntityUpdatedEventArgs? received = null;
        publisher.Updated += (_, args) => received = args;

        publisher.Publish(new EntityUpdatedEventArgs(entity, UserId, original));

        Assert.NotNull(received);
        Assert.Same(entity, received!.Entity);
        Assert.Same(original, received.OriginalEntity);
        Assert.Equal(SharedKernel.Bl.Enums.Events.EntityEnums.enCrudOp.Update, received.Operation);
    }

    [Fact]
    public void Publish_StatusChanged_RaisesStatusChangedEvent()
    {
        var publisher = new CrudPublisher();
        var entity = new TestEntity { Id = Guid.NewGuid() };
        EntityStatusChangedEventArgs? received = null;
        publisher.StatusChanged += (_, args) => received = args;

        publisher.Publish(new EntityStatusChangedEventArgs(
            entity,
            UserId,
            CurrentStateType.Active,
            CurrentStateType.InActive));

        Assert.NotNull(received);
        Assert.Same(entity, received!.Entity);
        Assert.Equal(CurrentStateType.Active, received.PreviousState);
        Assert.Equal(CurrentStateType.InActive, received.NewState);
        Assert.Equal(SharedKernel.Bl.Enums.Events.EntityEnums.enCrudOp.ChangeState, received.Operation);
    }

    [Fact]
    public void Publish_WithoutSubscribers_DoesNotThrow()
    {
        var publisher = new CrudPublisher();

        publisher.Publish(new EntityCreatedEventArgs(new TestEntity(), UserId));
        publisher.Publish(new EntityUpdatedEventArgs(new TestEntity(), UserId, new TestEntity()));
        publisher.Publish(new EntityStatusChangedEventArgs(
            new TestEntity(),
            UserId,
            CurrentStateType.Active,
            CurrentStateType.Deleted));

        Assert.True(true);
    }
}
