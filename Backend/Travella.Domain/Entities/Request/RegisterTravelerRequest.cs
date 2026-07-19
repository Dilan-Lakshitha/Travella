namespace Travella.API.Models
{
    public class RegisterTravelerRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public int? CompanyId { get; set; }
        public string CompanySlug { get; set; } = string.Empty;
    }
}
