using Claims.Models;
using Claims.Validation;

public class CreateClaimRequest
{
    public string CoverId { get; set; }

    public string Name { get; set; }

    public ClaimType Type { get; set; }
    [MaxDamageCost]
    public decimal DamageCost { get; set; }
}