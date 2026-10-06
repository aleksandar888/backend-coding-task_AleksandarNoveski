using System.ComponentModel.DataAnnotations;

namespace Claims.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class MaxDamageCostAttribute : ValidationAttribute
{
    private const decimal MaximumDamageCost = 100000m;

    public MaxDamageCostAttribute()
        : base("Damage cost cannot exceed 100.000.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        return value is decimal damageCost && damageCost <= MaximumDamageCost;
    }
}