using static SharedKernel.Domains.Types.CurrentState;
using static SharedKernel.Bl.Enums.Events.EntityEnums;

namespace SharedKernel.Bl.Events.Args;

public class EntityStatusChangedEventArgs : CrudEventArgs
{
    public CurrentStateType PreviousState { get; set; }
    public CurrentStateType NewState { get; set; }

    public EntityStatusChangedEventArgs(SharedKernelBaseTable entity, Guid userId, CurrentStateType previousState, CurrentStateType newState) : base(entity, userId)
    {
        Operation = enCrudOp.ChangeState;
        PreviousState = previousState;
        NewState = newState;
    }
}
