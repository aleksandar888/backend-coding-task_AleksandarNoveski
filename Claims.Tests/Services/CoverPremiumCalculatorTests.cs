using Claims.Models;
using Claims.Services;
using Xunit;

namespace Claims.Tests.Services;

public class CoverPremiumCalculatorTests
{
    private readonly CoverPremiumCalculator _calculator = new();

    [Theory]
    [InlineData(CoverType.Yacht, 1375.0)]
    [InlineData(CoverType.PassengerShip, 1500.0)]
    [InlineData(CoverType.Tanker, 1875.0)]
    [InlineData((CoverType)999, 1625.0)]
    public void Compute_ForOneDay_ReturnsRateForCoverType(
        CoverType coverType,
        decimal expected)
    {
        var result = _calculator.Compute(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 2),
            coverType);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Compute_WhenEndDateIsNotAfterStartDate_ReturnsZero(int dayOffset)
    {
        var startDate = new DateTime(2025, 1, 2);

        var result = _calculator.Compute(
            startDate,
            startDate.AddDays(dayOffset),
            CoverType.Yacht);

        Assert.Equal(0m, result);
    }

    [Fact]
    public void Compute_ForFractionalDay_CalculatesFractionalPremium()
    {
        var startDate = new DateTime(2025, 1, 1);

        var result = _calculator.Compute(
            startDate,
            startDate.AddHours(12),
            CoverType.Yacht);

        Assert.Equal(687.5m, result);
    }

    [Theory]
    [InlineData(30, 41250.0)]
    [InlineData(31, 42556.250)]
    [InlineData(180, 237187.50)]
    [InlineData(181, 238452.50)]
    [InlineData(365, 471212.50)]
    [InlineData(366, 472477.50)]
    public void Compute_ForYacht_AppliesTierRates(int days, decimal expected)
    {
        var startDate = days == 366
            ? new DateTime(2024, 1, 1)
            : new DateTime(2025, 1, 1);

        var result = _calculator.Compute(
            startDate,
            startDate.AddDays(days),
            CoverType.Yacht);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(31, 46470.0)]
    [InlineData(181, 266955.0)]
    public void Compute_ForNonYacht_AppliesTierDiscounts(int days, decimal expected)
    {
        var startDate = new DateTime(2025, 1, 1);

        var result = _calculator.Compute(
            startDate,
            startDate.AddDays(days),
            CoverType.PassengerShip);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Compute_ForUnknownCoverType_UsesDefaultMultiplierAndNonYachtDiscounts()
    {
        var startDate = new DateTime(2025, 1, 1);

        var result = _calculator.Compute(
            startDate,
            startDate.AddDays(31),
            (CoverType)999);

        Assert.Equal(50342.5m, result);
    }
}