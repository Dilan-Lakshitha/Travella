using System.Collections.Generic;

namespace Travella.Domain.Entities
{
    public class Staff
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Role { get; set; } = null!;

        public List<StaffAvailability> Availabilities { get; set; } = new();

        public List<ItineraryStaff> ItineraryStaff { get; set; } = new();
    }
}