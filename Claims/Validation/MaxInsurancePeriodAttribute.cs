using System.ComponentModel.DataAnnotations;
using Claims.Models;

namespace Claims.Validation;

[AttributeUsage(AttributeTargets.Class)]
public sealed class MaxInsurancePeriodAttribute : ValidationAttribute
{
    public MaxInsurancePeriodAttribute()
        : base("The total insurance period cannot exceed one year.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        return value is Cover cover &&
            (cover.StartDate.Year == DateTime.MaxValue.Year || cover.EndDate <= cover.StartDate.AddYears(1));
    }
}