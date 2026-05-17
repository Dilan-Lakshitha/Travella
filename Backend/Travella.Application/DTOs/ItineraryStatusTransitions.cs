using System;
using System.Collections.Generic;

namespace Travella.Application.DTOs
{
    public static class ItineraryStatusTransitions
    {
        private static readonly HashSet<string> StaffRejectFromStatuses =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "submitted",
                "under_review",
                "resubmitted",
            };

        private static readonly HashSet<string> StaffReturnFromStatuses =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "under_review",
                "resubmitted",
            };

        private static readonly HashSet<string> StaffStartReviewFromStatuses =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "submitted",
                "resubmitted",
                "under_review",
            };

        public static bool CanTravelerResubmit(string? status)
            => ItineraryStatusHelper.Normalize(status) == "returned_for_correction";

        public static bool CanStaffReject(string? status)
            => StaffRejectFromStatuses.Contains(ItineraryStatusHelper.Normalize(status));

        public static bool CanStaffReturnForCorrection(string? status)
            => StaffReturnFromStatuses.Contains(ItineraryStatusHelper.Normalize(status));

        public static bool CanStaffAssignReviewer(string? status)
            => StaffStartReviewFromStatuses.Contains(ItineraryStatusHelper.Normalize(status));

        public static bool IsLockedAfterRejection(string? status)
            => ItineraryStatusHelper.IsRejected(status);

        public static void EnsureTravelerResubmit(string? currentStatus)
        {
            if (!CanTravelerResubmit(currentStatus))
            {
                throw new InvalidOperationException(
                    "Only itineraries returned for correction can be resubmitted.");
            }
        }

        public static void EnsureStaffReject(string? currentStatus)
        {
            if (IsLockedAfterRejection(currentStatus))
            {
                throw new InvalidOperationException("Itinerary is already rejected.");
            }

            if (!CanStaffReject(currentStatus))
            {
                throw new InvalidOperationException(
                    "Reject is only allowed while itinerary is submitted, under review, or resubmitted.");
            }
        }

        public static void EnsureStaffReturnForCorrection(string? currentStatus)
        {
            if (IsLockedAfterRejection(currentStatus))
            {
                throw new InvalidOperationException("Rejected itineraries cannot be returned for correction.");
            }

            if (!CanStaffReturnForCorrection(currentStatus))
            {
                throw new InvalidOperationException(
                    "Return for correction is only allowed while itinerary is under review or resubmitted.");
            }
        }
    }
}
