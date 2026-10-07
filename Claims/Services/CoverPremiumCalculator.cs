using Claims.Models;

namespace Claims.Services;

public interface ICoverPremiumCalculator
{
    decimal Compute(DateTime startDate, DateTime endDate, CoverType coverType);
}

public sealed class CoverPremiumCalculator : ICoverPremiumCalculator
{
    public decimal Compute(DateTime startDate, DateTime endDate, CoverType coverType)
    {
        const decimal baseDayRate = 1250m;

        var typeMultiplier = coverType switch
        {
            CoverType.Yacht => 1.1m,
            CoverType.PassengerShip => 1.2m,
            CoverType.Tanker => 1.5m,
            _ => 1.3m
        };

        var premiumPerDay = baseDayRate * typeMultiplier;
        var insuranceLength = Math.Max(0m, (decimal)(endDate - startDate).TotalDays);
        var firstTierDays = Math.Min(insuranceLength, 30m);
        var secondTierDays = Math.Min(Math.Max(insuranceLength - 30m, 0m), 150m);
        var remainingDays = Math.Max(insuranceLength - 180m, 0m);

        var secondTierDiscount = coverType == CoverType.Yacht ? 0.05m : 0.02m;

        var finalTierDiscount = coverType == CoverType.Yacht ? 0.08m : 0.03m;

        return firstTierDays * premiumPerDay
            + secondTierDays * premiumPerDay * (1m - secondTierDiscount)
            + remainingDays * premiumPerDay * (1m - finalTierDiscount);
    }
}
