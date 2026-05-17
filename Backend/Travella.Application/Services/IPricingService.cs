using Travella.Application.DTOs;

namespace Travella.Application.Services
{
    public interface IPricingService
    {
        Task<ItineraryPricingDetailDto> CreatePricingAsync(ItineraryPricingInputDto dto, int createdBy, int companyId);

        Task<ItineraryPricingDetailDto?> GetPricingForItineraryAsync(int itineraryId, int companyId);

        Task<ItineraryPricingDetailDto> UpdateMarginAsync(UpdatePricingMarginDto dto, int companyId);
    }
}
