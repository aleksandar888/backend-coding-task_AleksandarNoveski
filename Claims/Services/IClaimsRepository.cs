namespace Claims.Services;

public interface IClaimsRepository
{
    Task<IReadOnlyList<Claim>> GetAllAsync();

    Task<Claim?> GetByIdAsync(string id);

    Task AddAsync(Claim claim);

    Task DeleteAsync(Claim claim);
}
