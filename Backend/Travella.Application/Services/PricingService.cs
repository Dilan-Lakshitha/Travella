using System;
using Travella.Application.DTOs;
using Travella.Application.Interfaces;

namespace Travella.Application.Services
{
    public class PricingService : IPricingService
    {
        private readonly IItineraryRepository _itineraryRepository;
        private readonly IPricingRepository _pricingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly IStaffEmailNotifier _staffEmailNotifier;

        public PricingService(
            IItineraryRepository itineraryRepository,
            IPricingRepository pricingRepository,
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            IStaffEmailNotifier staffEmailNotifier)
        {
            _itineraryRepository = itineraryRepository;
            _pricingRepository = pricingRepository;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _staffEmailNotifier = staffEmailNotifier;
        }

        public async Task<ItineraryPricingDetailDto> CreatePricingAsync(ItineraryPricingInputDto dto, int createdBy, int companyId)
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

            var status = itinerary.Status?.Trim().ToLowerInvariant() ?? string.Empty;
            if (status is not ("under_review" or "priced"))
            {
                throw new InvalidOperationException("Pricing can only be saved when the itinerary is under_review or priced.");
            }

            await _unitOfWork.BeginAsync();
            try
            {
                _ = await _pricingRepository.CreateAsync(dto, createdBy);
                await _itineraryRepository.UpdateItineraryStatusAsync(dto.ItineraryId, "priced");
                await _itineraryRepository.UpdateItineraryTotalPriceAsync(
                    dto.ItineraryId,
                    dto.TotalAmount);

                await _unitOfWork.CommitAsync();
                await _notificationService.NotifyItineraryPricedAsync(dto.ItineraryId, itinerary.GuestId);
                var priced = await _itineraryRepository.GetItineraryByIdAsync(dto.ItineraryId);
                if (priced != null)
                {
                    await _staffEmailNotifier.NotifyItineraryStatusChangedAsync(priced, companyId, "priced");
                }
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            var saved = await _pricingRepository.GetLatestByItineraryIdAsync(dto.ItineraryId);
            if (saved == null)
            {
                throw new InvalidOperationException("Pricing was saved but could not be loaded.");
            }

            return saved;
        }

        public async Task<ItineraryPricingDetailDto?> GetPricingForItineraryAsync(int itineraryId, int companyId)
        {
            var itinerary = await _itineraryRepository.GetItineraryByIdAsync(itineraryId);
            if (itinerary == null)
            {
                throw new InvalidOperationException("Itinerary not found.");
            }

            if (itinerary.CompanyId != companyId)
            {
                throw new InvalidOperationException("You can only view pricing within your company.");
            }

            return await _pricingRepository.GetLatestByItineraryIdAsync(itineraryId);
        }

        public async Task<ItineraryPricingDetailDto> UpdateMarginAsync(UpdatePricingMarginDto dto, int companyId)
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

            var status = itinerary.Status?.Trim().ToLowerInvariant() ?? string.Empty;
            if (status is not ("approved_by_admin" or "sent_to_admin"))
            {
                throw new InvalidOperationException("Profit margin can only be updated before final confirmation.");
            }

            var updated = await _pricingRepository.UpdateMarginAsync(dto.ItineraryId, dto.ProfitMargin);
            if (updated == null)
            {
                throw new InvalidOperationException("Pricing row not found for itinerary.");
            }

            await _itineraryRepository.UpdateItineraryTotalPriceAsync(dto.ItineraryId, updated.TotalAmount);
            return updated;
        }
    }
}
