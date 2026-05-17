namespace Travella.Application.DTOs
{
    public class AddAttractionDto
    {
        public int ItineraryDayId { get; set; }

        public int AttractionId { get; set; }

        public string? Description { get; set; }

        public decimal DurationHours { get; set; }
    }
}

