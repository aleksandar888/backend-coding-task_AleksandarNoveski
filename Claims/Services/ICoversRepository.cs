using Claims.Models;

namespace Claims.Services;

public interface ICoversRepository
{
    Task<IReadOnlyList<Cover>> GetAllAsync();

    Task<Cover?> GetByIdAsync(string id);

    Task AddAsync(Cover cover);

    Task DeleteAsync(Cover cover);
}
