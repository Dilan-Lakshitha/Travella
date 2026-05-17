using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Travella.Application.DTOs;
using Travella.Application.Interfaces;
using Travella.Domain.Entities;

namespace Travella.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IAuthRepository _authRepository;
        private readonly IItineraryRepository _itineraryRepository;
        private readonly INotificationNotifier _notifier;

        public NotificationService(
            INotificationRepository notificationRepository,
            IAuthRepository authRepository,
            IItineraryRepository itineraryRepository,
            INotificationNotifier notifier)
        {
            _notificationRepository = notificationRepository;
            _authRepository = authRepository;
            _itineraryRepository = itineraryRepository;
            _notifier = notifier;
        }

        public Task NotifyItineraryCreatedAsync(int itineraryId, int companyId)
        {
            return NotifyCompanyStaffAndAdminsAsync(
                companyId,
                itineraryId,
                NotificationTypes.ItineraryCreated,
                "New itinerary created",
                $"A traveler created a new itinerary (#{itineraryId}).");
        }

        public async Task NotifyItinerarySubmittedAsync(int itineraryId, int travelerId, int companyId, bool isResubmit)
        {
            if (isResubmit)
            {
                await PublishToUserAsync(
                    travelerId,
                    itineraryId,
                    NotificationTypes.ItineraryResubmitted,
                    "Itinerary resubmitted",
                    $"Your itinerary #{itineraryId} was resubmitted for review.");

                await NotifyCompanyStaffAndAdminsAsync(
                    companyId,
                    itineraryId,
                    NotificationTypes.ItineraryResubmitted,
                    "Itinerary resubmitted",
                    $"Itinerary #{itineraryId} was resubmitted by the traveler.");
            }
            else
            {
                await PublishToUserAsync(
                    travelerId,
                    itineraryId,
                    NotificationTypes.ItinerarySubmitted,
                    "Itinerary submitted",
                    $"Your itinerary #{itineraryId} was submitted for review.");

                await NotifyCompanyStaffAndAdminsAsync(
                    companyId,
                    itineraryId,
                    NotificationTypes.ItinerarySubmitted,
                    "Itinerary submitted",
                    $"Itinerary #{itineraryId} was submitted by a traveler.");
            }
        }

        public Task NotifyItineraryUnderReviewAsync(int itineraryId, int travelerId)
            => PublishToUserAsync(
                travelerId,
                itineraryId,
                NotificationTypes.ItineraryUnderReview,
                "Itinerary under review",
                $"Your itinerary #{itineraryId} is now under review.");

        public async Task NotifyItineraryReturnedForCorrectionAsync(int itineraryId, int travelerId, int reviewerUserId)
        {
            await PublishToUserAsync(
                travelerId,
                itineraryId,
                NotificationTypes.ItineraryReturnedForCorrection,
                "Itinerary returned for correction",
                $"Your itinerary #{itineraryId} was returned for correction. Please review staff feedback.");

            await PublishToUserAsync(
                reviewerUserId,
                itineraryId,
                NotificationTypes.ItineraryReturnedForCorrection,
                "Itinerary returned for correction",
                $"Itinerary #{itineraryId} was returned to the traveler for correction.");
        }

        public Task NotifyItineraryPricedAsync(int itineraryId, int travelerId)
            => PublishToUserAsync(
                travelerId,
                itineraryId,
                NotificationTypes.ItineraryPriced,
                "Itinerary priced",
                $"Your itinerary #{itineraryId} has been priced.");

        public async Task NotifyItinerarySentToAdminAsync(int itineraryId, int travelerId, int companyId)
        {
            await PublishToUserAsync(
                travelerId,
                itineraryId,
                NotificationTypes.ItinerarySentToAdmin,
                "Sent to owner/admin",
                $"Your itinerary #{itineraryId} was sent to the company owner for approval.");

            await NotifyCompanyAdminsAsync(
                companyId,
                itineraryId,
                NotificationTypes.ItinerarySentToAdmin,
                "Sent to owner/admin",
                $"Itinerary #{itineraryId} is awaiting owner approval.");
        }

        public Task NotifyItineraryApprovedAsync(int itineraryId, int travelerId)
            => PublishToUserAsync(
                travelerId,
                itineraryId,
                NotificationTypes.ItineraryApproved,
                "Approved",
                $"Your itinerary #{itineraryId} was approved.");

        public Task NotifyItineraryConfirmedAsync(int itineraryId, int travelerId)
            => PublishToUserAsync(
                travelerId,
                itineraryId,
                NotificationTypes.ItineraryConfirmed,
                "Confirmed",
                $"Your itinerary #{itineraryId} is confirmed.");

        public Task NotifyItineraryRejectedAsync(int itineraryId, int travelerId)
            => PublishToUserAsync(
                travelerId,
                itineraryId,
                NotificationTypes.ItineraryRejected,
                "Rejected",
                $"Your itinerary #{itineraryId} was rejected.");

        public async Task NotifyConversationMessageAsync(int itineraryId, int senderId, string senderRole, Itinerary itinerary)
        {
            var role = (senderRole ?? string.Empty).ToUpperInvariant();
            var assignedReviewerId = itinerary.AssignedReviewerId
                ?? await _itineraryRepository.GetAssignedReviewerIdAsync(itineraryId);

            var recipients = new List<int>();

            if (role == "TRAVELER")
            {
                if (ItineraryConversationRules.CanStaffViewConversation(itinerary.Status)
                    && assignedReviewerId.HasValue
                    && assignedReviewerId.Value != senderId)
                {
                    recipients.Add(assignedReviewerId.Value);
                }
            }
            else if (role is "STAFF" or "ADMIN")
            {
                if (ItineraryConversationRules.CanTravelerViewConversation(itinerary.Status)
                    && itinerary.GuestId != senderId)
                {
                    recipients.Add(itinerary.GuestId);
                }
            }

            foreach (var userId in recipients.Distinct())
            {
                await PublishToUserAsync(
                    userId,
                    itineraryId,
                    NotificationTypes.ConversationMessage,
                    "New conversation message",
                    $"New message on itinerary #{itineraryId}.");
            }
        }

        public Task<IReadOnlyList<NotificationDto>> GetForUserAsync(int userId, int limit = 50)
            => _notificationRepository.GetForUserAsync(userId, limit);

        public Task<int> GetUnreadCountAsync(int userId)
            => _notificationRepository.GetUnreadCountAsync(userId);

        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            await _notificationRepository.MarkAsReadAsync(notificationId, userId);
            var unread = await _notificationRepository.GetUnreadCountAsync(userId);
            await _notifier.NotifyUnreadCountAsync(userId, unread);
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            await _notificationRepository.MarkAllAsReadAsync(userId);
            await _notifier.NotifyUnreadCountAsync(userId, 0);
        }

        private async Task NotifyCompanyStaffAndAdminsAsync(
            int companyId,
            int itineraryId,
            string type,
            string title,
            string message)
        {
            var userIds = await GetCompanyStaffAndAdminUserIdsAsync(companyId);
            foreach (var userId in userIds)
            {
                await PublishToUserAsync(userId, itineraryId, type, title, message);
            }
        }

        private async Task NotifyCompanyAdminsAsync(
            int companyId,
            int itineraryId,
            string type,
            string title,
            string message)
        {
            var admins = await _authRepository.GetCompanyAdminUsersAsync(companyId);
            foreach (var (userId, _, _) in admins)
            {
                await PublishToUserAsync(userId, itineraryId, type, title, message);
            }
        }

        private async Task<List<int>> GetCompanyStaffAndAdminUserIdsAsync(int companyId)
        {
            var staff = await _authRepository.GetCompanyStaffUsersAsync(companyId);
            var admins = await _authRepository.GetCompanyAdminUsersAsync(companyId);
            return staff.Select(s => s.UserId)
                .Concat(admins.Select(a => a.UserId))
                .Distinct()
                .ToList();
        }

        private async Task PublishToUserAsync(
            int userId,
            int? itineraryId,
            string type,
            string title,
            string message)
        {
            var id = await _notificationRepository.CreateAsync(userId, itineraryId, type, title, message);
            var unread = await _notificationRepository.GetUnreadCountAsync(userId);
            var dto = new NotificationDto
            {
                Id = id,
                UserId = userId,
                ItineraryId = itineraryId,
                Type = type,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
            };

            await _notifier.NotifyUserAsync(userId, dto, unread);
        }
    }
}
