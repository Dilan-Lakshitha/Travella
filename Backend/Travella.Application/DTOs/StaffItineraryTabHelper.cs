using System;
using System.Collections.Generic;
using System.Linq;

namespace Travella.Application.DTOs
{
    public static class StaffItineraryTabHelper
    {
        private static readonly IReadOnlyDictionary<string, string[]> TabStatusMap =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["pending"] = new[] { "submitted" },
                ["in-review"] = new[] { "under_review" },
                ["returned"] = new[] { "returned_for_correction", "resubmitted" },
                ["priced"] = new[] { "priced", "sent_to_admin" },
                ["approved"] = new[] { "approved_by_staff" },
                ["completed"] = new[] { "approved_by_admin", "confirmed" },
                ["rejected"] = new[] { "rejected" },
            };

        public static bool TryResolveStatuses(string? tab, out string[] statuses)
        {
            statuses = Array.Empty<string>();
            if (string.IsNullOrWhiteSpace(tab))
            {
                return false;
            }

            return TabStatusMap.TryGetValue(tab.Trim(), out statuses!);
        }

        public static string NormalizeStatus(string? rawStatus)
            => ItineraryStatusHelper.Normalize(rawStatus);

        public static IReadOnlyCollection<string> AllTabKeys => TabStatusMap.Keys.ToList();
    }
}
