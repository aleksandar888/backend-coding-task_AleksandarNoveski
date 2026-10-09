using System.ComponentModel.DataAnnotations;
using Claims.Validation;
using Xunit;

namespace Claims.Tests.Validations;

public class NotInPastAttributeTests
{
    private readonly NotInPastAttribute _attribute = new();

    [Fact]
    public void GetValidationResult_WhenDateIsToday_ReturnsSuccess()
    {
        var result = Validate(DateTime.Today);

        Assert.Null(result);
    }

    [Fact]
    public void GetValidationResult_WhenDateIsInFuture_ReturnsSuccess()
    {
        var result = Validate(DateTime.Today.AddDays(1));

        Assert.Null(result);
    }

    [Fact]
    public void GetValidationResult_WhenDateIsBeforeToday_ReturnsError()
    {
        var result = Validate(DateTime.Today.AddDays(-1));

        Assert.NotNull(result);
        Assert.Equal("Start date cannot be in the past.", result.ErrorMessage);
    }

    [Fact]
    public void GetValidationResult_WhenTimeIsEarlierToday_ReturnsSuccess()
    {
        var result = Validate(DateTime.Today.AddHours(1).AddTicks(-1));

        Assert.Null(result);
    }

    [Fact]
    public void GetValidationResult_WhenValueIsNull_ReturnsSuccess()
    {
        var result = Validate(null);

        Assert.Null(result);
    }

    [Fact]
    public void GetValidationResult_WhenValueIsNotDateTime_ReturnsError()
    {
        var result = Validate("not a date");

        Assert.NotNull(result);
        Assert.Equal("Start date cannot be in the past.", result.ErrorMessage);
    }

    private ValidationResult? Validate(object? value) =>
        _attribute.GetValidationResult(value, new ValidationContext(new object()));
}
