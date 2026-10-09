using System.ComponentModel.DataAnnotations;
using Claims.Validation;
using Xunit;

namespace Claims.Tests.Validations;

public class MaxInsurancePeriodAttributeTests
{
    private readonly MaxInsurancePeriodAttribute _attribute = new();

    [Fact]
    public void GetValidationResult_WhenPeriodIsExactlyOneYear_ReturnsSuccess()
    {
        var startDate = new DateTime(2025, 1, 1);
        var result = Validate(new CreateCoverRequest
        {
            StartDate = startDate,
            EndDate = startDate.AddYears(1)
        });

        Assert.Null(result);
    }

    [Fact]
    public void GetValidationResult_WhenPeriodIsLessThanOneYear_ReturnsSuccess()
    {
        var startDate = new DateTime(2025, 1, 1);
        var result = Validate(new CreateCoverRequest
        {
            StartDate = startDate,
            EndDate = startDate.AddYears(1).AddDays(-1)
        });

        Assert.Null(result);
    }

    [Fact]
    public void GetValidationResult_WhenPeriodExceedsOneYear_ReturnsError()
    {
        var startDate = new DateTime(2025, 1, 1);
        var result = Validate(new CreateCoverRequest
        {
            StartDate = startDate,
            EndDate = startDate.AddYears(1).AddDays(1)
        });

        Assert.NotNull(result);
        Assert.Equal("The total insurance period cannot exceed one year.", result.ErrorMessage);
    }

    [Fact]
    public void GetValidationResult_WhenStartDateIsInMaximumYear_ReturnsSuccess()
    {
        var result = Validate(new CreateCoverRequest
        {
            StartDate = new DateTime(DateTime.MaxValue.Year, 1, 1),
            EndDate = DateTime.MaxValue
        });

        Assert.Null(result);
    }

    [Fact]
    public void GetValidationResult_WhenValueIsNull_ReturnsSuccess()
    {
        var result = _attribute.GetValidationResult(null, new ValidationContext(new object()));

        Assert.Null(result);
    }

    [Fact]
    public void GetValidationResult_WhenValueIsNotCreateCoverRequest_ReturnsError()
    {
        var result = _attribute.GetValidationResult(new object(), new ValidationContext(new object()));

        Assert.NotNull(result);
        Assert.Equal("The total insurance period cannot exceed one year.", result.ErrorMessage);
    }

    private ValidationResult? Validate(CreateCoverRequest request) =>
        _attribute.GetValidationResult(request, new ValidationContext(request));
}
