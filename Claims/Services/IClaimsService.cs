using Claims.Models;

namespace Claims.Services;

public interface IClaimsService
{
    Task<IReadOnlyList<Claim>> GetAllAsync();

    Task<Claim?> GetByIdAsync(string id);

    Task<(Claim? Claim, string? Error)> CreateAsync(Claim claim);

    Task<bool> DeleteAsync(string id);
}
