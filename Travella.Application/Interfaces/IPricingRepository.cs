namespace Travella.Application.Interfaces
{
    public interface IPricingRepository
    {
        Task<int> CreateAsync(int itineraryId, int createdBy, decimal totalAmount);
    }
}
