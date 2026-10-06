using Claims.Auditing;

namespace Claims.Services;

public sealed class ClaimsService : IClaimsService
{
    private readonly IClaimsRepository _repository;
    private readonly IAuditService _auditService;

    public ClaimsService(IClaimsRepository repository, IAuditService auditService)
    {
        _repository = repository;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<Claim>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public Task<Claim?> GetByIdAsync(string id)
    {
        return _repository.GetByIdAsync(id);
    }

    public async Task<Claim> CreateAsync(Claim claim)
    {
        claim.Id = Guid.NewGuid().ToString();
        await _repository.AddAsync(claim);
        await _auditService.AuditClaimAsync(claim.Id, "POST");
        return claim;
    }

    public async Task DeleteAsync(string id)
    {
        await _auditService.AuditClaimAsync(id, "DELETE");
        var claim = await GetByIdAsync(id);
        if (claim is null)
        {
            return;
        }

        await _repository.DeleteAsync(claim);
    }
}
