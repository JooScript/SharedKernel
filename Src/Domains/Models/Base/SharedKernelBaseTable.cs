using static SharedKernel.Domains.Types.CurrentState;

public abstract class SharedKernelBaseTable
{
    public required Guid Id { get; set; }

    public required CurrentStateType CurrentState { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid CreatedBy { get; set; } = default!;

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }
}
