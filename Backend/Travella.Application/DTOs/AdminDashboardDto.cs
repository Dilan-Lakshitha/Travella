using System.Collections.Generic;

namespace Travella.Application.DTOs
{
    public class AdminDashboardDto
    {
        public int TotalItineraries { get; set; }

        public int PendingReviewCount { get; set; }

        public int AwaitingApprovalCount { get; set; }

        public int ConfirmedCount { get; set; }

        public Dictionary<string, int> StatusCounts { get; set; } = new();

        public AdminDashboardSectionsDto Sections { get; set; } = new();
    }

    public class AdminDashboardSectionsDto
    {
        public List<ItineraryListItemDto> All { get; set; } = new();

        public List<ItineraryListItemDto> PendingReview { get; set; } = new();

        public List<ItineraryListItemDto> InReview { get; set; } = new();

        public List<ItineraryListItemDto> Returned { get; set; } = new();

        public List<ItineraryListItemDto> Priced { get; set; } = new();

        public List<ItineraryListItemDto> AwaitingApproval { get; set; } = new();

        public List<ItineraryListItemDto> Approved { get; set; } = new();

        public List<ItineraryListItemDto> Confirmed { get; set; } = new();

        public List<ItineraryListItemDto> Rejected { get; set; } = new();
    }
}
