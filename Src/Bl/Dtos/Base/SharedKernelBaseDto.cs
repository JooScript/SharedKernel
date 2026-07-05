using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Bl.Dtos.Base;

public abstract class SharedKernelBaseDto
{
    public Guid Id { get; set; } = Guid.Empty;

    public CurrentStateType CurrentState { get; set; } = CurrentStateType.Active;

}