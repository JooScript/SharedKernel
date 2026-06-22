using static SharedKernel.Domains.Enums.CurrentState;
using static SharedKernel.Bl.Enums.Events.EntityEnums;

namespace SharedKernel.Bl.Events.Args;

public class EntityStatusChangedEventArgs : CrudEventArgs
{
    public enCurrentState PreviousState { get; set; }
    public enCurrentState NewState { get; set; }

    public EntityStatusChangedEventArgs(SharedKernelBaseTable entity, Guid userId, enCurrentState previousState, enCurrentState newState) : base(entity, userId)
    {
        Operation = enCrudOp.ChangeState;
        PreviousState = previousState;
        NewState = newState;
    }
}
