using Travella.Domain.Entities;

namespace Travella.Application.DTOs
{
    public static class StaffResourceMapper
    {
        public static StaffResourceDto ToDto(Staff staff)
        {
            return new StaffResourceDto
            {
                Id = staff.Id,
                Name = staff.Name,
                Phone = staff.Phone,
                Experience = staff.Experience,
                Availability = staff.Availability,
                Language = staff.Language,
                Email = staff.Email,
                CompanyId = staff.CompanyId,
                Role = staff.Role,
            };
        }
    }
}
