namespace Travella.Application.Services
{
    public interface IApplicationService
    {
        Task<int> SubmitCompanyApplicationAsync(string companyName, string email, string phone);
    }
}
