using static SharedKernel.Core.Enums.Status.Status;

public abstract class SharedKernelBaseTable
{
    public Guid Id { get; set; } = default!;

    public enCurrentState CurrentState { get; set; } = enCurrentState.Active;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid CreatedBy { get; set; } = default!;

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }
}
