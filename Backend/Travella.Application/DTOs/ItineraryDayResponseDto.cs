using System.Collections.Generic;

namespace Travella.Application.DTOs
{
    public class ItineraryDayResponseDto
    {
        public int DayNumber { get; set; }

        public string OvernightLocation { get; set; } = null!;

        public List<ItineraryAttractionResponseDto> Attractions { get; set; } = new();
    }

    public class ItineraryAttractionResponseDto
    {
        public string Name { get; set; } = null!;

        public string? Address { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public string? Description { get; set; }
    }
}