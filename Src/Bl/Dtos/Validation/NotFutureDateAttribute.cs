using System.ComponentModel.DataAnnotations;

namespace SharedKernel.Bl.Dtos.Validation;

public class NotFutureDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            return ValidationResult.Success;

        value = value is DateTime dateTimeValue ? DateOnly.FromDateTime(dateTimeValue) : value;

        if (value is not DateOnly dateTime)
            return new ValidationResult("Invalid date value.");

        return dateTime > DateOnly.FromDateTime(DateTime.UtcNow)
            ? new ValidationResult(ErrorMessage ?? "Date must not be in the future.")
            : ValidationResult.Success;
    }
}
