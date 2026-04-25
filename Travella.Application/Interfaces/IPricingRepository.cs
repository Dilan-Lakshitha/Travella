using Travella.Application.DTOs;

namespace Travella.Application.Interfaces
{
    public interface IPricingRepository
    {
        Task<int> CreateAsync(ItineraryPricingInputDto dto, int createdBy);

        Task<bool> PricingExistsAsync(int itineraryId);

        Task<bool> UpdateMarginAsync(int itineraryId, decimal profitMargin);
    }
}
