namespace Travella.Domain.Entities.Auth
{
    public class AuthUserRecord
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? CompanyId { get; set; }
        public bool IsDeleted { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public bool MustChangePassword { get; set; }
    }
}
