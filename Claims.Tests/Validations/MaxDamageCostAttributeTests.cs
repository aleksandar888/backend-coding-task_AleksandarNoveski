using System.ComponentModel.DataAnnotations;
using Claims.Validation;
using Xunit;

namespace Claims.Tests.Validations;

public class MaxDamageCostAttributeTests
{
    private readonly MaxDamageCostAttribute _attribute = new();

    [Fact]
    public void GetValidationResult_WhenDamageCostIsAtOrBelowMaximum_ReturnsSuccess()
    {
        Assert.Null(Validate(0m));
        Assert.Null(Validate(50000m));
        Assert.Null(Validate(100000m));
    }

    [Fact]
    public void GetValidationResult_WhenDamageCostExceedsMaximum_ReturnsError()
    {
        var result = Validate(100000.01m);

        Assert.NotNull(result);
        Assert.Equal("Damage cost cannot exceed 100.000.", result.ErrorMessage);
    }

    [Fact]
    public void GetValidationResult_WhenDamageCostIsNegative_ReturnsSuccess()
    {
        var result = Validate(-1m);

        Assert.Null(result);
    }

    [Fact]
    public void GetValidationResult_WhenValueIsNull_ReturnsSuccess()
    {
        var result = Validate(null);

        Assert.Null(result);
    }

    [Fact]
    public void GetValidationResult_WhenValueIsNotDecimal_ReturnsError()
    {
        var result = Validate("100");

        Assert.NotNull(result);
        Assert.Equal("Damage cost cannot exceed 100.000.", result.ErrorMessage);
    }

    private ValidationResult? Validate(object? value) =>
        _attribute.GetValidationResult(value, new ValidationContext(new object()));
}
