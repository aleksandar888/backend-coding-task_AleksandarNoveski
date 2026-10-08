using Claims.Auditing;
using Claims.Logging;
using Claims.Models;

namespace Claims.Services;

public sealed class CoversService : ICoversService
{
    private readonly ICoversRepository _repository;
    private readonly IAuditService _auditService;
    private readonly ICoverPremiumCalculator _premiumCalculator;
    private readonly IErrorLoggingService _errorLoggingService;

    public CoversService(
        ICoversRepository repository,
        IAuditService auditService,
        ICoverPremiumCalculator premiumCalculator,
         IErrorLoggingService errorLoggingService)
    {
        _repository = repository;
        _auditService = auditService;
        _premiumCalculator = premiumCalculator;
        _errorLoggingService = errorLoggingService;
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
        try
        {
            cover.Id = Guid.NewGuid().ToString();
            cover.Premium = ComputePremium(cover.StartDate, cover.EndDate, cover.Type);
            await _repository.AddAsync(cover);
            await _auditService.AuditCoverAsync(cover.Id, "POST");
            return cover;
        }
        catch (Exception ex)
        {
            await _errorLoggingService.LogErrorAsync(ex, "Failed to create cover.", nameof(CoversService), nameof(CreateAsync));
            return new Cover();
        }
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
