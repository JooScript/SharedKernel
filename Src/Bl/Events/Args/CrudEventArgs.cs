using static SharedKernel.Bl.Enums.Events.EntityEnums;

namespace SharedKernel.Bl.Events.Args;

public class CrudEventArgs : EventArgs
{
    public CrudEventArgs(SharedKernelBaseTable entity, Guid userId)
    {
        Entity = entity;
        UserId = userId;
    }

    public enCrudOp Operation { get; protected set; } = enCrudOp.NotSpecified;
    public SharedKernelBaseTable Entity { get; protected set; }
    public DateTimeOffset EventTime { get; private set; } = DateTime.UtcNow;
    public Guid UserId { get; protected set; }

}
