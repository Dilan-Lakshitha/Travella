using System.Collections.Generic;
using System.Threading.Tasks;
using Travella.Application.DTOs;
using Travella.Domain.Entities;

namespace Travella.Application.Interfaces
{
    public interface IItineraryRepository
    {
        Task<int> CreateItineraryAsync(Itinerary itinerary);

        Task<bool> ItineraryExistsAsync(int itineraryId);

        Task<Itinerary?> GetItineraryByIdAsync(int itineraryId);

        Task<ItineraryResponseDto?> GetItineraryDetailsAsync(int itineraryId);

        Task AddItineraryDayAsync(ItineraryDay day);

        Task<ItineraryDay?> GetItineraryDayByIdAsync(int itineraryDayId);

        Task AddAttractionToDayAsync(ItineraryAttraction itineraryAttraction);

        Task AssignAccommodationAsync(ItineraryAccommodation itineraryAccommodation);

        Task<decimal> CalculateTotalPriceAsync(int itineraryId);

        Task UpdateItineraryTotalPriceAsync(int itineraryId, decimal totalPrice);

        Task UpdateItineraryStatusAsync(int itineraryId, string status);

        Task<List<ItineraryStaff>> GetItineraryStaffAsync(int itineraryId);

        Task AddItineraryStaffAsync(ItineraryStaff itineraryStaff);
    }
}