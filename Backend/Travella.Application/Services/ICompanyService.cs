namespace Travella.Application.Services
{
    public interface ICompanyService
    {
        Task<int> CreateCompanyAsync(string name, string email, string phone, int createdBy);
    }
}
