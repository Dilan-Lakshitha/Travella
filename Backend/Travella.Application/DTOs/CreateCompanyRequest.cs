using System;
using System.Collections.Generic;
using System.Text;

namespace Travella.Application.DTOs
{
    public class CreateCompanyRequest
    {
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string OwnerName { get; set; } = string.Empty;

        public string AdminEmail { get; set; } = string.Empty;

        public string? WebsiteUrl { get; set; }
    }
}
