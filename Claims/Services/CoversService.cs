using Claims.Auditing;

namespace Claims.Services;

public sealed class CoversService : ICoversService
{
    private readonly ICoversRepository _repository;
    private readonly IAuditService _auditService;
    private readonly ICoverPremiumCalculator _premiumCalculator;

    public CoversService(
        ICoversRepository repository,
        IAuditService auditService,
        ICoverPremiumCalculator premiumCalculator)
    {
        _repository = repository;
        _auditService = auditService;
        _premiumCalculator = premiumCalculator;
    }

    public async Task<IReadOnlyList<Cover>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public Task<Cover?> GetByIdAsync(string id)
    {
        return _repository.GetByIdAsync(id);
    }

    public async Task<Cover> CreateAsync(Cover cover)
    {
        cover.Id = Guid.NewGuid().ToString();
        cover.Premium = ComputePremium(cover.StartDate, cover.EndDate, cover.Type);
        await _repository.AddAsync(cover);
        await _auditService.AuditCoverAsync(cover.Id, "POST");
        return cover;
    }

    public async Task DeleteAsync(string id)
    {
        await _auditService.AuditCoverAsync(id, "DELETE");
        var cover = await GetByIdAsync(id);
        if (cover is null)
        {
            return;
        }

        await _repository.DeleteAsync(cover);
    }

    public decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType)
    {
        return _premiumCalculator.Compute(startDate, endDate, coverType);
    }
}
