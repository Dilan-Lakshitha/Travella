using Travella.Application.DTOs;

namespace Travella.Application.Interfaces
{
    public interface IPricingRepository
    {
        Task<int> CreateAsync(ItineraryPricingInputDto dto, int createdBy);

        Task<ItineraryPricingDetailDto?> GetLatestByItineraryIdAsync(int itineraryId);

        Task<Dictionary<int, ItineraryPricingDetailDto>> GetLatestByItineraryIdsAsync(IReadOnlyCollection<int> itineraryIds);

        Task<bool> PricingExistsAsync(int itineraryId);

        Task<ItineraryPricingDetailDto?> UpdateMarginAsync(int itineraryId, decimal profitMargin);
    }
}
