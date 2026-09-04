namespace SharedKernel.Domains.Types;

public abstract record StrEnum
{
    protected StrEnum(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}