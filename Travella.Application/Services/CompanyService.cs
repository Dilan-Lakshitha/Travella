using Travella.Application.Interfaces;

namespace Travella.Application.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly ICompanyRepository _companyRepository;

        public CompanyService(ICompanyRepository companyRepository)
        {
            _companyRepository = companyRepository;
        }

        public Task<int> CreateCompanyAsync(string name, string email, string phone, int createdBy)
            => _companyRepository.CreateAsync(name, email, phone, createdBy);
    }
}
