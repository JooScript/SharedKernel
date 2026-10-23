using Microsoft.EntityFrameworkCore;
using Moq;
using SharedKernel.Bl.Contracts.AuditLog;
using SharedKernel.Bl.Dtos.AuditLog;
using SharedKernel.Bl.Events.Args;
using SharedKernel.Bl.Events.Publishers;
using SharedKernel.Bl.Events.Subscribers;
using SharedKernel.Bl.Test.Infrastructure;
using SharedKernel.Domains.Types;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Bl.Test.Events;

public class AuditLogSubscriberTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<IAuditLog<DbContext>> _auditLog = new();

    private AuditLogSubscriber<DbContext> CreateSut()
    {
        _auditLog
            .Setup(a => a.AddAsync(It.IsAny<AuditLogDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SaveResultType.Ok(Guid.NewGuid()));

        return new AuditLogSubscriber<DbContext>(_auditLog.Object);
    }

    [Fact]
    public void Subscribe_ThenCreatedEvent_AddsAuditLog()
    {
        var sut = CreateSut();
        var publisher = new CrudPublisher();
        var entity = new TestEntity { Id = Guid.NewGuid() };
        sut.Subscribe(publisher);

        publisher.Publish(new EntityCreatedEventArgs(entity, UserId));

        _auditLog.Verify(
            a => a.AddAsync(
                It.Is<AuditLogDto>(d =>
                    d.Action == "Create" &&
                    d.Entity == nameof(TestEntity) &&
                    d.EntityId == entity.Id &&
                    d.CurrentState == CurrentStateType.Active &&
                    d.Details != null &&
                    d.Details.Contains(entity.Id.ToString())),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Subscribe_ThenUpdatedEvent_AddsAuditLog()
    {
        var sut = CreateSut();
        var publisher = new CrudPublisher();
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "New" };
        var original = new TestEntity { Id = entity.Id, Name = "Old" };
        sut.Subscribe(publisher);

        publisher.Publish(new EntityUpdatedEventArgs(entity, UserId, original));

        _auditLog.Verify(
            a => a.AddAsync(
                It.Is<AuditLogDto>(d =>
                    d.Action == "Update" &&
                    d.EntityId == entity.Id &&
                    d.Details != null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Subscribe_ThenStatusChangedEvent_AddsAuditLog()
    {
        var sut = CreateSut();
        var publisher = new CrudPublisher();
        var entity = new TestEntity { Id = Guid.NewGuid() };
        sut.Subscribe(publisher);

        publisher.Publish(new EntityStatusChangedEventArgs(
            entity,
            UserId,
            CurrentStateType.Active,
            CurrentStateType.InActive));

        _auditLog.Verify(
            a => a.AddAsync(
                It.Is<AuditLogDto>(d =>
                    d.Action == "ChangeState" &&
                    d.EntityId == entity.Id &&
                    d.Details != null &&
                    d.Details.Contains(CurrentStateType.Active.ToString()) &&
                    d.Details.Contains(CurrentStateType.InActive.ToString())),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void UnSubscribe_StopsAuditLogging()
    {
        var sut = CreateSut();
        var publisher = new CrudPublisher();
        sut.Subscribe(publisher);
        sut.UnSubscribe(publisher);

        publisher.Publish(new EntityCreatedEventArgs(new TestEntity(), UserId));

        _auditLog.Verify(
            a => a.AddAsync(It.IsAny<AuditLogDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
