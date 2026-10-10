
namespace SharedKernel.Domains.Types;

public record ValidationResult<T>(bool IsValid, List<T> Errors)
{
    public static ValidationResult<T> Success => new(true, []);

    public static ValidationResult<T> Fail(List<T> errors) => new(false, errors);
}

public record ValidationResult(bool IsValid, List<string> Errors)
    : ValidationResult<string>(IsValid, Errors)
{
    public new static ValidationResult Success => new(true, []);

    public new static ValidationResult Fail(List<string> errors) => new(false, errors);
}
