using System;

namespace Travella.Application.DTOs
{
    public static class NotificationTypes
    {
        public const string ItineraryCreated = "ITINERARY_CREATED";
        public const string ItinerarySubmitted = "ITINERARY_SUBMITTED";
        public const string ItineraryUnderReview = "ITINERARY_UNDER_REVIEW";
        public const string ItineraryReturnedForCorrection = "ITINERARY_RETURNED_FOR_CORRECTION";
        public const string ItineraryResubmitted = "ITINERARY_RESUBMITTED";
        public const string ItineraryPriced = "ITINERARY_PRICED";
        public const string ItinerarySentToAdmin = "ITINERARY_SENT_TO_ADMIN";
        public const string ItineraryApproved = "ITINERARY_APPROVED";
        public const string ItineraryConfirmed = "ITINERARY_CONFIRMED";
        public const string ItineraryRejected = "ITINERARY_REJECTED";
        public const string ConversationMessage = "CONVERSATION_MESSAGE";
    }

    public class NotificationDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int? ItineraryId { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
