using Travella.Application.Interfaces;

namespace Travella.Application.Services
{
    public class PricingService : IPricingService
    {
        private readonly IPricingRepository _pricingRepository;

        public PricingService(IPricingRepository pricingRepository)
        {
            _pricingRepository = pricingRepository;
        }

        public Task<int> CreatePricingAsync(int itineraryId, int createdBy, decimal totalAmount)
            => _pricingRepository.CreateAsync(itineraryId, createdBy, totalAmount);
    }
}
