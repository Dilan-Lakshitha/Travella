namespace Travella.Application.DTOs
{
    public class AuthUserDto
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? CompanyId { get; set; }
    }
}
