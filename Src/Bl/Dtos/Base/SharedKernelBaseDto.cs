using static SharedKernel.Domains.Enums.CurrentState;

namespace SharedKernel.Bl.Dtos.Base;

public abstract class SharedKernelBaseDto
{
    public Guid Id { get; set; } = Guid.Empty;

    public enCurrentState CurrentState { get; set; } = enCurrentState.Active;

}