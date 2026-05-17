using System;

namespace Travella.Application.DTOs
{
    public class CreateItineraryDto
    {
        public int GuestId { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public int? CompanyId { get; set; }
    }
}
