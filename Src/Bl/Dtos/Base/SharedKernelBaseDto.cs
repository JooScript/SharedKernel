using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Bl.Dtos.Base;

public abstract class SharedKernelBaseDto
{
    public required Guid? Id { get; set; }

    public required CurrentStateType CurrentState { get; set; } = CurrentStateType.Active;

}