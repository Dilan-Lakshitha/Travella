using Travella.Application.DTOs;

namespace Travella.Application.Services
{
    public interface IPricingService
    {
        Task<int> CreatePricingAsync(ItineraryPricingInputDto dto, int createdBy, int companyId);

        Task UpdateMarginAsync(UpdatePricingMarginDto dto, int companyId);
    }
}
