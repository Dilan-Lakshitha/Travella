using System.Collections.Generic;
using System.Threading.Tasks;
using Travella.Application.DTOs;
using Travella.Domain.Entities;

namespace Travella.Application.Interfaces
{
    public interface INotificationService
    {
        Task NotifyItineraryCreatedAsync(int itineraryId, int companyId);

        Task NotifyItinerarySubmittedAsync(int itineraryId, int travelerId, int companyId, bool isResubmit);

        Task NotifyItineraryUnderReviewAsync(int itineraryId, int travelerId);

        Task NotifyItineraryReturnedForCorrectionAsync(int itineraryId, int travelerId, int reviewerUserId);

        Task NotifyItineraryPricedAsync(int itineraryId, int travelerId);

        Task NotifyItinerarySentToAdminAsync(int itineraryId, int travelerId, int companyId);

        Task NotifyItineraryApprovedAsync(int itineraryId, int travelerId);

        Task NotifyItineraryConfirmedAsync(int itineraryId, int travelerId);

        Task NotifyItineraryRejectedAsync(int itineraryId, int travelerId);

        Task NotifyConversationMessageAsync(int itineraryId, int senderId, string senderRole, Itinerary itinerary);

        Task<IReadOnlyList<NotificationDto>> GetForUserAsync(int userId, int limit = 50);

        Task<int> GetUnreadCountAsync(int userId);

        Task MarkAsReadAsync(int notificationId, int userId);

        Task MarkAllAsReadAsync(int userId);
    }
}
