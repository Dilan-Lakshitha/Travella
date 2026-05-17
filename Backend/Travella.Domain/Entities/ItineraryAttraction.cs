namespace Travella.Domain.Entities
{
    public class ItineraryAttraction
    {
        public int Id { get; set; }

        public int ItineraryDayId { get; set; }

        public int AttractionId { get; set; }

        public string? Description { get; set; }

        public decimal DurationHours { get; set; }

        // Navigation properties
        public ItineraryDay? ItineraryDay { get; set; }

        public Attraction? Attraction { get; set; }
    }
}