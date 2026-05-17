using System;
using System.Collections.Generic;

namespace Travella.Application.DTOs
{
    public static class ItineraryConversationRules
    {
        private static readonly HashSet<string> ConversationStatuses =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "returned_for_correction",
                "resubmitted",
            };

        public static bool CanTravelerViewConversation(string? status)
        {
            return ConversationStatuses.Contains(ItineraryStatusHelper.Normalize(status));
        }

        public static bool CanStaffViewConversation(string? status)
        {
            return ConversationStatuses.Contains(ItineraryStatusHelper.Normalize(status));
        }

        public static (bool CanView, bool CanSend) ResolveForTraveler(string? status, int? assignedReviewerId)
        {
            if (!CanTravelerViewConversation(status))
            {
                return (false, false);
            }

            _ = assignedReviewerId;
            return (true, true);
        }

        public static (bool CanView, bool CanSend) ResolveForStaff(
            string? status,
            int staffUserId,
            int? assignedReviewerId)
        {
            if (!CanStaffViewConversation(status))
            {
                return (false, false);
            }

            var isAssignedReviewer = assignedReviewerId.HasValue && assignedReviewerId.Value == staffUserId;
            return (true, isAssignedReviewer);
        }
    }
}
