using Travella.Domain.Domain;

namespace Travella.Domain.Interface
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<int> CreateAsync(User user);
    }
}
