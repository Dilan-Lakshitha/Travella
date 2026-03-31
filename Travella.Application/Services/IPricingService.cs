namespace Travella.Application.Services
{
    public interface IPricingService
    {
        Task<int> CreatePricingAsync(int itineraryId, int createdBy, decimal totalAmount);
    }
}
