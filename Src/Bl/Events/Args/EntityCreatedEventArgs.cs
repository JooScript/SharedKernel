using static SharedKernel.Bl.Enums.Events.EntityEnums;

namespace SharedKernel.Bl.Events.Args;

public class EntityCreatedEventArgs : CrudEventArgs
{
    public EntityCreatedEventArgs(SharedKernelBaseTable entity, Guid userId) : base(entity, userId)
    {
        Operation = enCrudOp.Create;
    }
}
