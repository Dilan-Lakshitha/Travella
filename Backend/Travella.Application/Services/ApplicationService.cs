using Travella.Application.Interfaces;

namespace Travella.Application.Services
{
    public class ApplicationService : IApplicationService
    {
        private readonly IApplicationRepository _applicationRepository;

        public ApplicationService(IApplicationRepository applicationRepository)
        {
            _applicationRepository = applicationRepository;
        }

        public Task<int> SubmitCompanyApplicationAsync(string companyName, string email, string phone)
            => _applicationRepository.CreateAsync(companyName, email, phone);
    }
}
