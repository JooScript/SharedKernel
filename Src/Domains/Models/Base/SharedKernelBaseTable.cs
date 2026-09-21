using static SharedKernel.Domains.Types.CurrentState;

public abstract class SharedKernelBaseTable
{
    public Guid Id { get; set; } = default!;

    public CurrentStateType CurrentState { get; set; } = CurrentStateType.Active;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid CreatedBy { get; set; } = default!;

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }
}
