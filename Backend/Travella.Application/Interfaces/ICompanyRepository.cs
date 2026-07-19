using Travella.Application.DTOs;

namespace Travella.Application.Interfaces
{
    public interface ICompanyRepository
    {
        Task<CreatedCompanyAdminResult> CreateCompanyWithAdminAsync(CreateCompanyRequest request,string slug,string passwordHash,int? createdBy);
        Task<bool> CompanyEmailExistsAsync(string email);
        Task<bool> UserEmailExistsAsync(string email);
        Task<bool> SlugExistsAsync(string slug);
        Task<int> CreateAsync(CreateCompanyApplicationRequest request);
        Task<bool> HasPendingApplicationAsync(string email);
    }
}
