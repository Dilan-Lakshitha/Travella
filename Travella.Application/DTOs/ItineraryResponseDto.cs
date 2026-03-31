using System;
using System.Collections.Generic;

namespace Travella.Application.DTOs
{
    public class ItineraryResponseDto
    {
        public int Id { get; set; }

        public int GuestId { get; set; }

        public string GuestName { get; set; } = null!;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Status { get; set; } = null!;

        public decimal TotalPrice { get; set; }

        public List<ItineraryDayResponseDto> Days { get; set; } = new();

        public List<ItineraryStaffSummaryDto> Staff { get; set; } = new();
    }


    public class ItineraryStaffSummaryDto
    {
        public int StaffId { get; set; }

        public string StaffName { get; set; } = null!;

        public string Role { get; set; } = null!;
    }
}