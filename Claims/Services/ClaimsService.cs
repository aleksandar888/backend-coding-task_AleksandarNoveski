using Claims.Auditing;
using Claims.Models;
using System.ComponentModel.DataAnnotations;

namespace Claims.Services;

public sealed class ClaimsService : IClaimsService
{
    private readonly IClaimsRepository _repository;
    private readonly ICoversRepository _coversRepository;
    private readonly IAuditService _auditService;

    public ClaimsService(
        IClaimsRepository repository,
        ICoversRepository coversRepository,
        IAuditService auditService)
    {
        _repository = repository;
        _coversRepository = coversRepository;
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
        var cover = await _coversRepository.GetByIdAsync(claim.CoverId);
        if (cover is null)
        {
            throw new ValidationException("Please select a valid cover.");
        }
        else if (claim.Created < cover.StartDate || claim.Created > cover.EndDate)
        {
            throw new ValidationException("Claim created date must be within the related cover's insurance period.");
        }

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
