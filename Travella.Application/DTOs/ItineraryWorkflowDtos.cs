using System;
using System.Collections.Generic;

namespace Travella.Application.DTOs
{
    public class ItineraryListItemDto
    {
        public int Id { get; set; }

        // Stored/derived metadata for dashboards
        public int GuestId { get; set; }
        public string GuestName { get; set; } = string.Empty;

        public string TripName { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public int DaysCount { get; set; }

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        // Raw DB status (tbl_itineraries.status)
        public string RawStatus { get; set; } = string.Empty;

        // UI-friendly status (draft/submitted/approved/confirmed/corrected/rejected)
        public string Status { get; set; } = string.Empty;

        public DateOnly? SubmittedDate { get; set; }

        public int? CompanyId { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? LastMessagePreview { get; set; }
    }

    public class UpdateItineraryStatusDto
    {
        public int ItineraryId { get; set; }
    }

    public class AddItineraryReviewDto
    {
        public int ItineraryId { get; set; }
        public string Comments { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDING";
    }

    public class AddItineraryPricingDto
    {
        public int ItineraryId { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ItineraryPricingInputDto
    {
        public int ItineraryId { get; set; }
        public decimal DriverCost { get; set; }
        public decimal GuideCost { get; set; }
        public decimal VehicleCost { get; set; }
        public decimal MileageRate { get; set; }
        public decimal TotalKm { get; set; }
        public decimal AccommodationCost { get; set; }
        public string MealPlan { get; set; } = "BB";
        public decimal ProfitMargin { get; set; }
        public decimal TotalAmount { get; set; }
    }
    public class AssignItineraryStaffDto
    {
        public int ItineraryId { get; set; }
        public int DriverId { get; set; }
        public int GuideId { get; set; }
    }

    public class ItineraryMessageDto
    {
        public int Id { get; set; }
        public int ItineraryId { get; set; }
        public int SenderId { get; set; }
        public string SenderRole { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "COMMENT";
        public DateTime CreatedAt { get; set; }
    }

    public class AddItineraryMessageDto
    {
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "COMMENT";
    }

    public class UpdatePricingMarginDto
    {
        public int ItineraryId { get; set; }
        public decimal ProfitMargin { get; set; }
    }
}
