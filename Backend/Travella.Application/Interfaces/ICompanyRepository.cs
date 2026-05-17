namespace Travella.Application.Interfaces
{
    public interface ICompanyRepository
    {
        Task<int> CreateAsync(string name, string email, string phone, int createdBy);
    }
}
