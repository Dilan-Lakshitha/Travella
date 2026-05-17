using System.Collections.Generic;

namespace Travella.Domain.Entities
{
    public class ItineraryDay
    {
        public int Id { get; set; }

        public int ItineraryId { get; set; }

        public int DayNumber { get; set; }

        public string OvernightLocation { get; set; } = null!;

        public Itinerary? Itinerary { get; set; }

        public List<ItineraryAttraction> Attractions { get; set; } = new();

        public List<ItineraryAccommodation> Accommodations { get; set; } = new();
    }
}