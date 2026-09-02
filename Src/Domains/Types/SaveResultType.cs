namespace SharedKernel.Domains.Types;

public record SaveResultType<TError>(
    bool Success,
    Guid Id,
    IReadOnlyList<TError> Errors)
{
    public static SaveResultType<TError> From(
        bool success,
        Guid id,
        params TError[] errors)
        => success ? Ok(id) : Fail(errors);
    public static SaveResultType<TError> Ok(Guid id)
        => new(true, id, []);

    public static SaveResultType<TError> Fail(params TError[] errors)
        => new(false, Guid.Empty, errors);
}

public record SaveResultType(
    bool Success,
    Guid Id,
    IReadOnlyList<string> Errors)
    : SaveResultType<string>(Success, Id, Errors)
{
    public new static SaveResultType From(
        bool success,
        Guid id,
        params string[] errors)
        => success ? Ok(id) : Fail(errors);

    public new static SaveResultType Ok(Guid id)
        => new(true, id, []);

    public new static SaveResultType Fail(params string[] errors)
        => new(false, Guid.Empty, errors);
}