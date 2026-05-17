using System.Collections.Generic;
using System.Threading.Tasks;
using Travella.Application.DTOs;

namespace Travella.Application.Services
{
    public interface IItineraryService
    {
        Task<SaveGoogleAttractionResponseDto> SaveGoogleAttractionAsync(SaveGoogleAttractionDto dto);

        Task<int> CreateItineraryAsync(ItineraryDraftUpsertDto dto, int guestId, int companyId);

        Task SaveItineraryDraftAsync(int itineraryId, ItineraryDraftUpsertDto dto, int travelerId);

        Task<ItineraryFullResponseDto?> GetItineraryFullAsync(int itineraryId, int userId, string role, int? companyId);

        Task<int> AddDayAsync(AddItineraryDayDto dto, int travelerId);

        Task AddAttractionAsync(AddAttractionDto dto, int travelerId);

        Task AssignAccommodationAsync(AssignAccommodationDto dto);

        Task AssignStaffAsync(AssignStaffDto dto, int companyId);

        Task<List<ItineraryListItemDto>> GetGuestItinerariesAsync(int guestId);

        Task<List<ItineraryListItemDto>> GetSubmittedItinerariesAsync(int companyId);

        Task<List<ItineraryListItemDto>> GetStaffItinerariesByTabAsync(int companyId, string tab);

        Task<List<ItineraryListItemDto>> GetCompanyItinerariesAsync(int companyId);

        Task<AdminDashboardDto> GetAdminDashboardAsync(int companyId);

        Task<List<ItineraryListItemDto>> GetOwnerSubmittedItinerariesAsync();

        Task SubmitItineraryAsync(int itineraryId, int travelerId);

        Task ResubmitItineraryAsync(int itineraryId, int travelerId);

        Task DeleteDraftItineraryAsync(int itineraryId, int travelerId);

        Task MarkUnderReviewAsync(int itineraryId, int companyId, int staffUserId);

        Task<AssignReviewerResultDto> AssignReviewerAsync(int itineraryId, int companyId, int staffUserId);

        Task AssignDriverGuideAsync(int itineraryId, int driverId, int guideId, int companyId);

        Task ApproveItineraryAsync(int itineraryId, string approverRole, int companyId);

        Task ConfirmItineraryAsync(int itineraryId, int companyId);

        Task RejectItineraryAsync(int itineraryId, int companyId, int? actorUserId = null, string? actorRole = null);

        Task SendToAdminAsync(int itineraryId, int companyId);

        Task<ItineraryConversationDto> GetItineraryConversationAsync(int itineraryId, int userId, string role, int? companyId);

        Task<ItineraryMessageDto> AddItineraryMessageAsync(int itineraryId, int senderId, string senderRole, int? companyId, AddItineraryMessageDto dto);

        Task RequestCorrectionAsync(int itineraryId, int senderId, string senderRole, int companyId, string message);

        Task ReturnItineraryForCorrectionAsync(int itineraryId, int senderId, string senderRole, int companyId, string message);
    }
}
