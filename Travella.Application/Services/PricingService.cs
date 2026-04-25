using Travella.Application.Interfaces;
using System;
using Travella.Application.DTOs;

namespace Travella.Application.Services
{
    public class PricingService : IPricingService
    {
        private readonly IItineraryRepository _itineraryRepository;
        private readonly IPricingRepository _pricingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PricingService(
            IItineraryRepository itineraryRepository,
            IPricingRepository pricingRepository,
            IUnitOfWork unitOfWork)
        {
            _itineraryRepository = itineraryRepository;
            _pricingRepository = pricingRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> CreatePricingAsync(ItineraryPricingInputDto dto, int createdBy, int companyId)
        {
            var itinerary = await _itineraryRepository.GetItineraryByIdAsync(dto.ItineraryId);
            if (itinerary == null)
            {
                throw new InvalidOperationException("Itinerary not found.");
            }

            if (itinerary.CompanyId != companyId)
            {
                throw new InvalidOperationException("You can only price itineraries within your company.");
            }

            if (!string.Equals(itinerary.Status, "under_review", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Pricing can only be created for itineraries that are under_review.");
            }

            await _unitOfWork.BeginAsync();
            try
            {
                var pricingId = await _pricingRepository.CreateAsync(dto, createdBy);
                await _itineraryRepository.UpdateItineraryStatusAsync(dto.ItineraryId, "priced");
                await _unitOfWork.CommitAsync();
                return pricingId;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

        }

        public async Task UpdateMarginAsync(UpdatePricingMarginDto dto, int companyId)
        {
            var itinerary = await _itineraryRepository.GetItineraryByIdAsync(dto.ItineraryId);
            if (itinerary == null)
            {
                throw new InvalidOperationException("Itinerary not found.");
            }

            if (itinerary.CompanyId != companyId)
            {
                throw new InvalidOperationException("You can only update pricing within your company.");
            }

            if (!string.Equals(itinerary.Status, "approved_by_admin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(itinerary.Status, "sent_to_admin", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Margin update is only allowed for sent_to_admin/approved_by_admin itineraries.");
            }

            var updated = await _pricingRepository.UpdateMarginAsync(dto.ItineraryId, dto.ProfitMargin);
            if (!updated)
            {
                throw new InvalidOperationException("Pricing row not found for itinerary.");
            }
        }
    }
}
