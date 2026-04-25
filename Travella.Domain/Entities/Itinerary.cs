using System;
using System.Collections.Generic;

namespace Travella.Domain.Entities
{
    public class Itinerary
    {
        public int Id { get; set; }

        public int GuestId { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public string Status { get; set; } = null!;

        public decimal TotalPrice { get; set; }

        public int? CompanyId { get; set; }

        public User? Guest { get; set; }

        public List<ItineraryDay> Days { get; set; } = new();

        public List<ItineraryStaff> ItineraryStaff { get; set; } = new();

        public Booking? Booking { get; set; }
    }
}