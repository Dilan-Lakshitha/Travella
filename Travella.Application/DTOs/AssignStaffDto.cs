using System;

namespace Travella.Application.DTOs
{
    public class AssignStaffDto
    {
        public int ItineraryId { get; set; }

        public int StaffId { get; set; }

        public string? RequiredRole { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }
    }
}