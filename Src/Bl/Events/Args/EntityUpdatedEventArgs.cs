using static SharedKernel.Bl.Enums.Events.EntityEnums;

namespace SharedKernel.Bl.Events.Args;

public class EntityUpdatedEventArgs : CrudEventArgs
{
    public SharedKernelBaseTable OriginalEntity { get; }

    public EntityUpdatedEventArgs(SharedKernelBaseTable entity, Guid userId, SharedKernelBaseTable originalEntity) : base(entity, userId)
    {
        Operation = enCrudOp.Update;
        OriginalEntity = originalEntity;
    }

}
