using System.ComponentModel.DataAnnotations;
using Claims.Validation;
using Xunit;

namespace Claims.Tests.Validations;

public class EndDateAfterAttributeTests
{
    private readonly EndDateAfterAttribute _attribute = new(nameof(DateRange.StartDate));

    [Fact]
    public void GetValidationResult_WhenEndDateIsLaterThanStartDate_ReturnsSuccess()
    {
        var startDate = new DateTime(2025, 1, 1);
        var result = Validate(
            startDate.AddDays(1),
            new DateRange { StartDate = startDate });

        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetValidationResult_WhenEndDateIsNotLaterThanStartDate_ReturnsError(int dayOffset)
    {
        var startDate = new DateTime(2025, 1, 1);
        var result = Validate(
            startDate.AddDays(dayOffset),
            new DateRange { StartDate = startDate });

        Assert.NotNull(result);
        Assert.Equal("EndDate must be later than StartDate.", result.ErrorMessage);
    }

    [Fact]
    public void GetValidationResult_WhenValueIsNotDateTime_ReturnsSuccess()
    {
        var result = Validate("not a date", new DateRange { StartDate = new DateTime(2025, 1, 1) });

        Assert.Null(result);
    }

    [Fact]
    public void GetValidationResult_WhenStartDatePropertyIsMissing_ReturnsSuccess()
    {
        var result = _attribute.GetValidationResult(
            new DateTime(2025, 1, 2),
            new ValidationContext(new object()) { MemberName = "EndDate" });

        Assert.Null(result);
    }

    [Fact]
    public void GetValidationResult_WhenStartDateIsNotDateTime_ReturnsSuccess()
    {
        var result = Validate(
            new DateTime(2025, 1, 2),
            new InvalidDateRange { StartDate = "not a date" });

        Assert.Null(result);
    }

    [Fact]
    public void GetValidationResult_WhenCustomErrorMessageIsSet_UsesCustomMessage()
    {
        var attribute = new EndDateAfterAttribute(nameof(DateRange.StartDate))
        {
            ErrorMessage = "Custom date validation message."
        };

        var result = attribute.GetValidationResult(
            new DateTime(2025, 1, 1),
            new ValidationContext(new DateRange { StartDate = new DateTime(2025, 1, 1) })
            {
                MemberName = "EndDate",
                DisplayName = "EndDate"
            });

        Assert.NotNull(result);
        Assert.Equal("Custom date validation message.", result.ErrorMessage);
    }

    private ValidationResult? Validate(object? value, object instance) =>
        _attribute.GetValidationResult(
            value,
            new ValidationContext(instance)
            {
                MemberName = "EndDate",
                DisplayName = "EndDate"
            });

    private sealed class DateRange
    {
        public DateTime StartDate { get; init; }
    }

    private sealed class InvalidDateRange
    {
        public object? StartDate { get; init; }
    }
}
