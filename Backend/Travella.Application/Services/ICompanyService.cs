using Travella.Application.DTOs;

namespace Travella.Application.Services
{
    public interface ICompanyService
    {
        Task<CreateCompanyResponse> CreateCompanyAsync(CreateCompanyRequest request, int? createdBy);
        Task<int> SubmitApplicationAsync(CreateCompanyApplicationRequest request);
    }
}
