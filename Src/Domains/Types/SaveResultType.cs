namespace SharedKernel.Domains.Types;

public record SaveResultType(
    bool Success,
    Guid Id,
    string? ErrorMessage = null)
{
    public static SaveResultType Ok(Guid id)
        => new(true, id, null);

    public static SaveResultType Fail(string? errorMessage = null)
        => new(false, Guid.Empty, errorMessage);
}