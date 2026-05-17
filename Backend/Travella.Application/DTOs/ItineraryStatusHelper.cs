using System;
using System.Collections.Generic;

namespace Travella.Application.DTOs
{
    public static class ItineraryStatusHelper
    {
        public static string Normalize(string? rawStatus)
        {
            return string.IsNullOrWhiteSpace(rawStatus)
                ? string.Empty
                : rawStatus.Trim().ToLowerInvariant();
        }

        public static string GetDisplayLabel(string? rawStatus)
        {
            return Normalize(rawStatus) switch
            {
                "submitted" => "Pending Review",
                "under_review" => "In Review",
                "returned_for_correction" => "Returned",
                "resubmitted" => "Resubmitted",
                "priced" => "Priced",
                "approved_by_staff" => "Approved by Staff",
                "sent_to_admin" => "Awaiting Approval",
                "approved_by_admin" => "Approved",
                "confirmed" => "Confirmed",
                "rejected" => "Rejected",
                "draft" => "Draft",
                _ => string.IsNullOrWhiteSpace(rawStatus) ? "Unknown" : rawStatus!
            };
        }

        public static bool IsTravelerEditable(string? rawStatus)
        {
            var status = Normalize(rawStatus);
            return status is "draft" or "returned_for_correction";
        }

        public static bool IsFinalReadOnly(string? rawStatus)
        {
            var status = Normalize(rawStatus);
            return status is "approved_by_admin" or "confirmed";
        }

        public static bool IsRejected(string? rawStatus)
            => Normalize(rawStatus) == "rejected";

        public static bool CanStaffPerformReviewActions(string? rawStatus)
        {
            var status = Normalize(rawStatus);
            return status is "submitted" or "under_review" or "resubmitted";
        }

        public static IReadOnlySet<string> TravelerSubmittedTabStatuses { get; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "submitted",
                "under_review",
            };

        public static IReadOnlySet<string> TravelerReturnedTabStatuses { get; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "returned_for_correction",
                "resubmitted",
            };

        public static IReadOnlySet<string> TravelerApprovedTabStatuses { get; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "priced",
                "approved_by_staff",
                "sent_to_admin",
                "approved_by_admin",
                "confirmed",
            };
    }
}
