using Claims.Models;

namespace Claims.Services;

public interface ICoversService
{
    Task<IReadOnlyList<Cover>> GetAllAsync();

    Task<Cover?> GetByIdAsync(string id);

    Task<Cover> CreateAsync(Cover cover);

    Task DeleteAsync(string id);

    decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType);
}
