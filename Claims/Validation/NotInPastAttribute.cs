using System.ComponentModel.DataAnnotations;

namespace Claims.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class NotInPastAttribute : ValidationAttribute
{
    public NotInPastAttribute()
        : base("Start date cannot be in the past.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        return value is DateTime date && date.Date >= DateTime.Today;
    }
}
