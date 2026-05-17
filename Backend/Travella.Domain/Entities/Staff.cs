using System.Collections.Generic;

namespace Travella.Domain.Entities
{
    public class Staff
    {
        public int Id { get; set; }

        public int? CompanyId { get; set; }

        public string Name { get; set; } = null!;

        public string Role { get; set; } = null!;

        public string? Phone { get; set; }

        public int? Experience { get; set; }

        public string? Availability { get; set; }

        public string? Language { get; set; }

        public string? Email { get; set; }

        public List<StaffAvailability> Availabilities { get; set; } = new();

        public List<ItineraryStaff> ItineraryStaff { get; set; } = new();
    }
}