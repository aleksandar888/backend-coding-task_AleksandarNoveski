using Claims.Models;
using Claims.Validation;

namespace Claims.Contracts.Requests;

[MaxInsurancePeriod]
public sealed class CreateCoverRequest
{
    [NotInPast]
    public DateTime StartDate { get; set; }
    [EndDateAfter(nameof(StartDate))]
    public DateTime EndDate { get; set; }
    public CoverType Type { get; set; }
}
