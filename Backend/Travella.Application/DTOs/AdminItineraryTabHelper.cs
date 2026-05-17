using System;
using System.Collections.Generic;
using System.Linq;

namespace Travella.Application.DTOs
{
    public static class AdminItineraryTabHelper
    {
        public static readonly string[] WorkflowStatuses =
        {
            "draft",
            "submitted",
            "under_review",
            "returned_for_correction",
            "resubmitted",
            "priced",
            "approved_by_admin",
            "confirmed",
            "rejected",
        };

        private static readonly IReadOnlyDictionary<string, string[]> TabStatusMap =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["all"] = Array.Empty<string>(),
                ["pending-review"] = new[] { "submitted" },
                ["in-review"] = new[] { "under_review" },
                ["returned"] = new[] { "returned_for_correction", "resubmitted" },
                ["priced"] = new[] { "priced" },
                ["awaiting-approval"] = new[] { "sent_to_admin" },
                ["approved"] = new[] { "approved_by_admin" },
                ["confirmed"] = new[] { "confirmed" },
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

        public static IReadOnlyCollection<string> AllTabKeys => TabStatusMap.Keys.ToList();

        public static IReadOnlyList<string> FilterItemsByTab(string tab, IEnumerable<string> rawStatuses)
        {
            if (!TryResolveStatuses(tab, out var statuses) || statuses.Length == 0)
            {
                return rawStatuses.ToList();
            }

            var set = new HashSet<string>(statuses, StringComparer.OrdinalIgnoreCase);
            return rawStatuses.Where(s => set.Contains(ItineraryStatusHelper.Normalize(s))).ToList();
        }
    }

}
