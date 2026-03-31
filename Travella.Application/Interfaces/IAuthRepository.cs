using Travella.Domain.Entities.Auth;

namespace Travella.Application.Interfaces
{
    public interface IAuthRepository
    {
        Task<AuthUserRecord?> GetByEmailAsync(string email);
        Task<int> CreateTravelerAsync(string name, string email, string passwordHash);
    }
}
