using System;

namespace Travella.Application.DTOs
{
    public class AssignStaffDto
    {
        public int ItineraryId { get; set; }

        public int StaffId { get; set; }

        public string? RequiredRole { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }
}