using System;

namespace Travella.Domain.Entities
{
    public class StaffAvailability
    {
        public int Id { get; set; }

        public int StaffId { get; set; }

        public DateTime Date { get; set; }

        public string Status { get; set; } = null!;

        public Staff? Staff { get; set; }
    }
}