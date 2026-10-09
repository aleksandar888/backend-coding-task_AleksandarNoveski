using Claims.Models;
using Claims.Validation;

[MaxInsurancePeriod]
public sealed class CreateCoverRequest
{
    [NotInPast]
    public DateTime StartDate { get; set; }
    [EndDateAfter(nameof(StartDate))]
    public DateTime EndDate { get; set; }
    public CoverType Type { get; set; }
}
