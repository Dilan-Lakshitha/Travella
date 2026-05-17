namespace Travella.Application.Interfaces
{
    public interface IApplicationRepository
    {
        Task<int> CreateAsync(string companyName, string email, string phone);
    }
}
