using System.ComponentModel.DataAnnotations;

namespace Claims.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class EndDateAfterAttribute(string startDateProperty) : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        if (value is not DateTime endDate)
            return ValidationResult.Success;

        var property = validationContext.ObjectType.GetProperty(startDateProperty);
        if (property?.GetValue(validationContext.ObjectInstance) is not DateTime startDate)
            return ValidationResult.Success;

        return endDate > startDate
            ? ValidationResult.Success
            : new ValidationResult(
                ErrorMessage ?? $"{validationContext.DisplayName} must be later than {startDateProperty}.");
    }
}
