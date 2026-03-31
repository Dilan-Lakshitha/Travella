using System;

namespace Travella.Application.DTOs
{
    public class CreateItineraryDto
    {
        public int GuestId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }
}
