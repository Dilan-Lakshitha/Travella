using System.Collections.Generic;
using System.Threading.Tasks;
using Travella.Application.DTOs;
using Travella.Domain.Entities;

namespace Travella.Application.Interfaces
{
    public interface IItineraryRepository
    {
        Task<SaveGoogleAttractionResponseDto> GetOrCreateGoogleAttractionAsync(SaveGoogleAttractionDto dto);

        Task<int> CreateItineraryAsync(Itinerary itinerary);

        Task<bool> ItineraryExistsAsync(int itineraryId);

        Task<Itinerary?> GetItineraryByIdAsync(int itineraryId);

        Task<ItineraryFullResponseDto?> GetItineraryFullAsync(int itineraryId);

        Task DeleteItineraryNestedContentAsync(int itineraryId);

        Task UpdateItineraryDatesAsync(int itineraryId, System.DateOnly startDate, System.DateOnly endDate);

        Task<int> EnsureMealPlanIdAsync(string? mealPlanCode);

        Task<int> EnsureAccommodationIdAsync(string? accommodationType);

        Task<int> CountItineraryDaysAsync(int itineraryId);

        Task<int> AddItineraryDayAsync(ItineraryDay day);

        Task<ItineraryDay?> GetItineraryDayByIdAsync(int itineraryDayId);

        Task AddAttractionToDayAsync(ItineraryAttraction itineraryAttraction);

        Task AssignAccommodationAsync(ItineraryAccommodation itineraryAccommodation);

        Task<decimal> CalculateTotalPriceAsync(int itineraryId);

        Task UpdateItineraryTotalPriceAsync(int itineraryId, decimal totalPrice);

        Task UpdateItineraryStatusAsync(int itineraryId, string status);

        Task<List<ItineraryStaff>> GetItineraryStaffAsync(int itineraryId);

        Task AddItineraryStaffAsync(ItineraryStaff itineraryStaff);

        Task<List<ItineraryListItemDto>> GetGuestItinerariesAsync(int guestId);

        Task<List<ItineraryListItemDto>> GetSubmittedItinerariesAsync(int companyId);

        Task<List<ItineraryListItemDto>> GetCompanyItinerariesAsync(int companyId);

        Task<List<ItineraryListItemDto>> GetAllOwnerSubmittedItinerariesAsync();

        Task<string?> GetLatestReviewStatusAsync(int itineraryId);

        Task DeleteDraftItineraryAsync(int itineraryId, int travelerId);

        Task<List<ItineraryMessageDto>> GetItineraryMessagesAsync(int itineraryId);

        Task<int> AddItineraryMessageAsync(int itineraryId, int senderId, string senderRole, string message, string type);

        Task<string?> GetLastItineraryMessageAsync(int itineraryId);
    }
}
