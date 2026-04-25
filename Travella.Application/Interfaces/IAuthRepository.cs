using Travella.Domain.Entities.Auth;

namespace Travella.Application.Interfaces
{
    public interface IAuthRepository
    {
        Task<AuthUserRecord?> GetByEmailAsync(string email);
        Task<int> CreateTravelerAsync(string name, string email, string passwordHash, int companyId);

        Task<int> CreateStaffUserAsync(string name, string email, int companyId, string passwordHash, bool mustChangePassword);

        Task<bool> UpdatePasswordAsync(string email, string newPasswordHash, bool mustChangePassword);

        Task<List<(int UserId, string Name, string Email)>> GetCompanyStaffUsersAsync(int companyId);
    }
}
