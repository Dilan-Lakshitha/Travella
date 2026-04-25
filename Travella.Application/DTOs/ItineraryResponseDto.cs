using System;
using System.Collections.Generic;

namespace Travella.Application.DTOs
{
    public class ItineraryResponseDto
    {
        public int Id { get; set; }

        public int GuestId { get; set; }

        public string GuestName { get; set; } = null!;

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public string Status { get; set; } = null!;

        public List<ItineraryDayResponseDto> Days { get; set; } = new();
    }
}