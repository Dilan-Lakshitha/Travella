using System.Threading.Tasks;
using Travella.Application.DTOs;

namespace Travella.Application.Services
{
    public interface IItineraryService
    {
        Task<int> CreateItineraryAsync(CreateItineraryDto dto);

        Task<ItineraryResponseDto?> GetItineraryAsync(int itineraryId);

        Task AddDayAsync(AddItineraryDayDto dto);

        Task AddAttractionAsync(AddAttractionDto dto);

        Task AssignAccommodationAsync(AssignAccommodationDto dto);

        Task AssignStaffAsync(AssignStaffDto dto);
    }
}

