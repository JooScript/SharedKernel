using System.ComponentModel.DataAnnotations;

namespace SharedKernel.Bl.Dtos.Validation;

public sealed class GreaterThanZeroAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        return value switch
        {
            decimal v => v > 0,
            double v => v > 0,
            float v => v > 0,
            int v => v > 0,
            long v => v > 0,
            _ => false
        };
    }
}