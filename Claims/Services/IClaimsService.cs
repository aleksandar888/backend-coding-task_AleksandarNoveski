using Claims.Models;

namespace Claims.Services;

public interface IClaimsService
{
    Task<IReadOnlyList<Claim>> GetAllAsync();

    Task<Claim?> GetByIdAsync(string id);

    Task<Claim> CreateAsync(Claim claim);

    Task DeleteAsync(string id);
}
