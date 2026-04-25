using System;
using System.Collections.Generic;

namespace Travella.Application.DTOs
{
    public class ItineraryDraftUpsertDto
    {
        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public List<ItineraryDraftDayDto> Days { get; set; } = new();
    }

    public class ItineraryDraftDayDto
    {
        public int DayNumber { get; set; }

        public string OvernightLocation { get; set; } = string.Empty;

        public string? MealPlanCode { get; set; }

        public string? AccommodationType { get; set; }

        public List<ItineraryDraftAttractionDto> Attractions { get; set; } = new();
    }

    public class ItineraryDraftAttractionDto
    {
        public int AttractionId { get; set; }

        public string? Description { get; set; }

        public decimal DurationHours { get; set; } = 2m;
    }

    public class ItineraryHeaderDto
    {
        public int Id { get; set; }

        public int GuestId { get; set; }

        public string GuestName { get; set; } = string.Empty;

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal TotalPrice { get; set; }

        public int? CompanyId { get; set; }
    }

    public class ItineraryDayFlatDto
    {
        public int Id { get; set; }

        public int DayNumber { get; set; }

        public string OvernightLocation { get; set; } = string.Empty;
    }

    public class ItineraryAttractionFlatDto
    {
        public int Id { get; set; }

        public int ItineraryDayId { get; set; }

        public int AttractionId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Address { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public string? Description { get; set; }

        public decimal DurationHours { get; set; }
    }

    public class ItineraryAccommodationFlatDto
    {
        public int Id { get; set; }

        public int ItineraryDayId { get; set; }

        public int AccommodationId { get; set; }

        public string? AccommodationName { get; set; }

        public int MealPlanId { get; set; }

        public string? MealPlanCode { get; set; }
    }

    public class ItineraryFullResponseDto
    {
        public ItineraryHeaderDto Itinerary { get; set; } = null!;

        public List<ItineraryDayFlatDto> Days { get; set; } = new();

        public List<ItineraryAttractionFlatDto> Attractions { get; set; } = new();

        public List<ItineraryAccommodationFlatDto> Accommodations { get; set; } = new();
    }
}
