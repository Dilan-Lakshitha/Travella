using System.Collections.Generic;

namespace Travella.Application.DTOs
{
    public class ItineraryDayResponseDto
    {
        public int Id { get; set; }

        public int DayNumber { get; set; }

        public string OvernightLocation { get; set; } = null!;

        public List<ItineraryAttractionResponseDto> Attractions { get; set; } = new();

        public List<ItineraryAccommodationResponseDto> Accommodations { get; set; } = new();
    }

    public class ItineraryAttractionResponseDto
    {
        public int Id { get; set; }

        public int AttractionId { get; set; }

        public string AttractionName { get; set; } = null!;

        public string City { get; set; } = null!;

        public string Country { get; set; } = null!;

        public string? Description { get; set; }

        public decimal DurationHours { get; set; }
    }

    public class ItineraryAccommodationResponseDto
    {
        public int Id { get; set; }

        public int AccommodationId { get; set; }

        public string AccommodationName { get; set; } = null!;

        public string Location { get; set; } = null!;

        public decimal PricePerNight { get; set; }

        public int MealPlanId { get; set; }

        public string MealPlanCode { get; set; } = null!;
    }
}