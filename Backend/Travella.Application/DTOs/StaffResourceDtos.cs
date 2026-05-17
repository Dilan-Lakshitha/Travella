namespace Travella.Application.DTOs
{
    public class StaffResourceDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public int? Experience { get; set; }

        public string? Availability { get; set; }

        public string? Language { get; set; }

        public string? Email { get; set; }

        public int? CompanyId { get; set; }

        public string Role { get; set; } = string.Empty;
    }
}
