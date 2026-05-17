using System;

namespace Travella.Domain.Entities
{
    public class Booking
    {
        public int Id { get; set; }

        public int ItineraryId { get; set; }

        public DateTime ConfirmedAt { get; set; }

        public string InvoiceNumber { get; set; } = null!;

        public Itinerary? Itinerary { get; set; }
    }
}