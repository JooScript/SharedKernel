using Microsoft.EntityFrameworkCore;
using SharedKernel.Bl.Contracts.AuditLog;
using SharedKernel.Bl.Contracts.Events;
using SharedKernel.Bl.Dtos.AuditLog;
using SharedKernel.Bl.Events.Args;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Bl.Events.Subscribers;

public class AuditLogSubscriber<TDbContext>(IAuditLog<TDbContext> auditLog)
    : IAuditLogSubscriber
    where TDbContext : DbContext

{
    public void Subscribe<TPublisher>(TPublisher publisher) where TPublisher : ICrudPublisher
    {
        publisher.Created += Handle;
        publisher.Updated += Handle;
        publisher.StatusChanged += Handle;
    }

    public void UnSubscribe<TPublisher>(TPublisher publisher) where TPublisher : ICrudPublisher
    {
        publisher.Created -= Handle;
        publisher.Updated -= Handle;
        publisher.StatusChanged -= Handle;
    }

    private void Handle<TArgs>(object? sender, TArgs args) where TArgs : CrudEventArgs
    {
        var entityName = args.Entity.GetType().Name;

        var details = args switch
        {
            EntityCreatedEventArgs e => $"New item created at ID {e.Entity.Id} in entity {entityName}",
            EntityUpdatedEventArgs e => $"Entity {entityName} with ID {e.Entity.Id} changed from {e.OriginalEntity.ToString()} to {e.Entity.ToString()}",
            EntityStatusChangedEventArgs e => $"Entity {entityName} with ID {e.Entity.Id} state changed from {e.PreviousState} to {e.NewState}",
            _ => throw new InvalidOperationException($"Unsupported event type: {args.GetType().Name}")
        };

        AuditLogDto? dto = new AuditLogDto
        {
            Id = Guid.NewGuid(),
            Action = args.Operation.ToString(),
            EntityId = args.Entity.Id,
            Entity = entityName,
            CurrentState = CurrentStateType.Active,
            Details = details
        };

        if (dto != null)
        {
            auditLog.AddAsync(dto, CancellationToken.None).GetAwaiter().GetResult();
        }

    }

}