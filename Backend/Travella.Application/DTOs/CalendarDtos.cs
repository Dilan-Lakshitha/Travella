using System;

namespace Travella.Application.DTOs
{
    public class StaffBookingCalendarItemDto
    {
        public int StaffId { get; set; }

        public string StaffName { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string? Language { get; set; }

        public string? Email { get; set; }

        /// <summary>AVAILABLE when no BOOKED rows overlap the filter range; BOOKED otherwise.</summary>
        public string AvailabilityStatus { get; set; } = "AVAILABLE";

        public int? ItineraryId { get; set; }

        public string? ItineraryTitle { get; set; }

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public string? Status { get; set; }

        public string? BookedStatus { get; set; }

        public string? BookedDateRange { get; set; }
    }

    public class ItineraryBookingCalendarItemDto
    {
        public int ItineraryId { get; set; }

        public string ItineraryTitle { get; set; } = string.Empty;

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public string ItineraryStatus { get; set; } = string.Empty;

        public string? BookedDateRange { get; set; }

        public int? DriverId { get; set; }

        public string? DriverName { get; set; }

        public string? DriverLanguage { get; set; }

        public string? DriverEmail { get; set; }

        public int? GuideId { get; set; }

        public string? GuideName { get; set; }

        public string? GuideLanguage { get; set; }

        public string? GuideEmail { get; set; }
    }
}
