using static SharedKernel.Core.Enums.Status.Status;

namespace SharedKernel.Bl.Dtos.Base;

public abstract class SharedKernelBaseDto
{
    public Guid Id { get; set; } = Guid.Empty;

    public enCurrentState CurrentState { get; set; } = enCurrentState.Active;

}