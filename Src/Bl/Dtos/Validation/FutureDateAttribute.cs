using System.ComponentModel.DataAnnotations;

namespace SharedKernel.Bl.Dtos.Validation;

public class FutureDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            return ValidationResult.Success;

        if (value is not DateTime dateTime)
            return new ValidationResult("Invalid datetime value.");

        return dateTime > DateTime.UtcNow
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage ?? "Date must be in the future.");
    }
}